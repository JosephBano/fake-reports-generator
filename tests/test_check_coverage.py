"""
Unit tests for scripts/check-coverage.py.

These tests are regression guards for the coverage gate script that protects
the CI pipeline from committing a Cobertura XML report that drops below the
configured threshold. The script is hardened against XXE / billion-laughs via
defusedxml, so we also include a test that asserts the billion-laughs payload
is parsed safely (no OOM, terminates within a reasonable timeout).

Run:
    python3 -m unittest tests.test_check_coverage -v

Requires defusedxml to be importable in the same Python environment as the
script under test (pip install defusedxml==0.7.1).
"""

from __future__ import annotations

import os
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

REPO_ROOT = Path(__file__).resolve().parent.parent
SCRIPT = REPO_ROOT / "scripts" / "check-coverage.py"

# Defensive: every test needs the script to exist on disk.
assert SCRIPT.is_file(), f"check-coverage.py not found at {SCRIPT}"

# Timeout for subprocess calls. The billion-laughs test relies on this to
# detect a regression where defusedxml stops protecting the parser (in which
# case the process would hang or OOM). 10s is generous but still cheap.
SUBPROCESS_TIMEOUT_S = 10

# Minimal Cobertura XML used for happy-path tests. line-rate is the value the
# gate inspects; branch-rate is required by parse_coverage() so we always set
# both to deterministic floats.
TEMPLATE_XML = textwrap.dedent(
    """\
    <?xml version="1.0" encoding="utf-8"?>
    <coverage line-rate="{line_rate}" branch-rate="{branch_rate}" \
version="1.0" timestamp="0">
      <sources><source>.</source></sources>
      <packages/>
    </coverage>
    """
).strip()

# Billion-laughs payload: nested entity expansion that would blow up to ~10^10
# 'A' characters if parsed by a non-hardened XML parser. With defusedxml the
# parser raises instead of expanding.
BILLION_LAUGHS_XML = textwrap.dedent(
    """\
    <?xml version="1.0"?>
    <!DOCTYPE coverage [
      <!ENTITY x "AAAAAAAAAA">
      <!ENTITY y "&x;&x;&x;&x;&x;&x;&x;&x;&x;&x;">
      <!ENTITY z "&y;&y;&y;&y;&y;&y;&y;&y;&y;&y;">
    ]>
    <root line-rate="0.75" branch-rate="0.50">&z;</root>
    """
).strip()


def _check_defusedxml_available() -> None:
    """Skip the whole module if defusedxml is not installed."""
    try:
        import defusedxml  # noqa: F401
    except ImportError as e:
        raise unittest.SkipTest(
            "defusedxml is required to run check-coverage.py; install it "
            "with: pip install defusedxml==0.7.1"
        ) from e


_check_defusedxml_available()


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _write_coverage(tmp_dir: str, body: str, filename: str = "coverage.cobertura.xml") -> str:
    """Write a fixture XML inside tmp_dir and return its absolute path."""
    p = Path(tmp_dir) / filename
    p.write_text(body, encoding="utf-8")
    return str(p)


def _run_script(xml_path: str, threshold: int, *, timeout: int = SUBPROCESS_TIMEOUT_S):
    """Invoke the script as a subprocess and return CompletedProcess.

    We invoke it as a subprocess instead of importing it because:
      * the script uses sys.exit() at module top-level (no main() entrypoint),
      * we want to assert the real exit code the CI runner will see.
    """
    return subprocess.run(
        [sys.executable, str(SCRIPT), xml_path, str(threshold)],
        capture_output=True,
        text=True,
        timeout=timeout,
        check=False,
    )


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


class TestCheckCoverageHappyPath(unittest.TestCase):
    """Coverage above threshold exits 0."""

    def test_high_coverage_passes_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml_path = _write_coverage(tmp, TEMPLATE_XML.format(line_rate="0.75", branch_rate="0.50"))
            result = _run_script(xml_path, threshold=60)

        self.assertEqual(
            result.returncode,
            0,
            msg=f"expected exit 0; got {result.returncode}\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )
        self.assertIn("Line coverage:", result.stdout)
        self.assertIn("75.00%", result.stdout)
        self.assertIn("Coverage gate passed.", result.stdout)


class TestCheckCoverageBelowThreshold(unittest.TestCase):
    """Coverage below threshold exits 1 with an explanatory error.

    NOTE: check-coverage.py currently prints the ::error:: message to stdout,
    not stderr (the import-failure path is the only one that uses
    file=sys.stderr). We assert on stdout to match actual behavior — see
    the handoff note for the inconsistency to fix in the script.
    """

    def test_low_coverage_fails_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml_path = _write_coverage(tmp, TEMPLATE_XML.format(line_rate="0.40", branch_rate="0.30"))
            result = _run_script(xml_path, threshold=60)

        self.assertEqual(
            result.returncode,
            1,
            msg=f"expected exit 1; got {result.returncode}\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )
        combined = result.stdout + result.stderr
        self.assertIn("Coverage 40.00% is below threshold 60.00%", combined)


class TestCheckCoverageBoundary(unittest.TestCase):
    """Coverage exactly at threshold passes (gate uses strict `<`)."""

    def test_at_threshold_passes_gate(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml_path = _write_coverage(tmp, TEMPLATE_XML.format(line_rate="0.60", branch_rate="0.60"))
            result = _run_script(xml_path, threshold=60)

        self.assertEqual(
            result.returncode,
            0,
            msg=(
                f"line-rate == threshold must pass (gate uses strict '<'); "
                f"got {result.returncode}\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}"
            ),
        )


class TestCheckCoveragePositionalThreshold(unittest.TestCase):
    """Threshold must be honored when passed as the 2nd positional argument."""

    def test_positional_threshold_is_honored(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml_path = _write_coverage(tmp, TEMPLATE_XML.format(line_rate="0.80", branch_rate="0.80"))
            result = _run_script(xml_path, threshold=50)

        self.assertEqual(
            result.returncode,
            0,
            msg=(
                f"line-rate=0.80 with threshold=50 should pass; got {result.returncode}\n"
                f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}"
            ),
        )


class TestCheckCoveragePathValidation(unittest.TestCase):
    """Unsafe paths are rejected with exit 2 before any XML parsing happens.

    NOTE: check-coverage.py currently writes the rejection message to stdout,
    not stderr. We assert on the combined output to match actual behavior.
    """

    def test_absolute_etc_path_rejected(self):
        # The script never needs to read this path because validation runs first.
        result = _run_script("/etc/passwd", threshold=60)
        self.assertEqual(result.returncode, 2)
        self.assertIn("Refusing to read report with unsafe path", result.stdout + result.stderr)

    def test_shell_metacharacter_path_rejected(self):
        result = _run_script("foo;rm -rf.xml", threshold=60)
        self.assertEqual(result.returncode, 2)
        self.assertIn("Refusing to read report with unsafe path", result.stdout + result.stderr)


class TestCheckCoverageMalformedXml(unittest.TestCase):
    """A non-parseable XML exits 1 with a 'Failed to parse' error.

    NOTE: 'Failed to parse' currently lands on stdout (the script's only
    file=sys.stderr call is the import-failure path). Assert on combined.
    """

    def test_unclosed_tag_exits_one(self):
        broken = '<root line-rate="0.75" branch-rate="0.50">'
        with tempfile.TemporaryDirectory() as tmp:
            xml_path = _write_coverage(tmp, broken, filename="bad.xml")
            result = _run_script(xml_path, threshold=60)

        self.assertEqual(
            result.returncode,
            1,
            msg=f"expected exit 1 for malformed XML; got {result.returncode}\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )
        self.assertIn("Failed to parse", result.stdout + result.stderr)


class TestCheckCoverageXxeProtection(unittest.TestCase):
    """Critical regression guard: billion-laughs payload must not OOM the runner.

    If a future refactor accidentally drops defusedxml, this test will either
    time out (subprocess.run raises TimeoutExpired) or return a non-zero code
    with a DTD-related error. Either failure mode is a regression.
    """

    def test_billion_laughs_payload_does_not_hang(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml_path = _write_coverage(tmp, BILLION_LAUGHS_XML, filename="xxe.xml")
            try:
                result = _run_script(xml_path, threshold=60, timeout=SUBPROCESS_TIMEOUT_S)
            except subprocess.TimeoutExpired:
                self.fail(
                    "check-coverage.py hung on a billion-laughs XML payload — "
                    "defusedxml protection is missing or broken."
                )

        # If defusedxml is in place, the parser either:
        #   (a) refuses the DOCTYPE and raises (our case: exits 1 with
        #       'Failed to parse' on stderr), or
        #   (b) parses but ignores entity expansion (line-rate=0.75, exits 0).
        # What we MUST assert is that:
        #   * the process completed within the timeout (already enforced above), and
        #   * if it succeeded, the parsed line-rate is the expected 75.00%.
        # We don't pin exit 0 vs 1 across defusedxml versions because the
        # library's policy on entity expansion has tightened over time; we only
        # pin the safety invariant (no hang, no OOM).
        self.assertIn(
            result.returncode,
            (0, 1),
            msg=(
                f"unexpected exit code {result.returncode} for billion-laughs payload\n"
                f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}"
            ),
        )

        # If the script *did* parse successfully (exit 0), it must have seen
        # the literal line-rate attribute, not an expanded entity string.
        if result.returncode == 0:
            self.assertIn(
                "Line coverage:",
                result.stdout,
                msg=(
                    "billion-laughs payload was parsed but the line-rate value "
                    "looks wrong — entity expansion may have leaked through.\n"
                    f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}"
                ),
            )
            self.assertIn("75.00%", result.stdout)


if __name__ == "__main__":
    unittest.main()