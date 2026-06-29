#!/usr/bin/env python3
"""
Coverage gate check for .NET Cobertura XML reports.

Hardened against XXE / billion-laughs / quadratic-blowup attacks via defusedxml.
Even if a malicious Cobertura XML is committed to the repo, the script refuses to
expand external entities or run into OOM.

Usage:
    python3 scripts/check-coverage.py <path-to-coverage.cobertura.xml> <threshold-percent>

Exit code 0 if coverage >= threshold, 1 otherwise.
"""

import os
import re
import sys

try:
    from defusedxml.ElementTree import parse as xml_parse
except ImportError:
    print(
        "::error::defusedxml is not installed. "
        "Run: pip install defusedxml",
        file=sys.stderr,
    )
    sys.exit(2)


def _err(msg: str) -> None:
    """Print a ::error:: line to stderr."""
    print(msg, file=sys.stderr)


def _notice(msg: str) -> None:
    """Print a ::notice:: line to stdout."""
    print(msg)


# Allow only safe path characters and require .xml extension to prevent
# shell-quoting/redirect shenanigans if a future workflow caller passes user input.
_PATH_PATTERN = re.compile(r"^[A-Za-z0-9._/-]+\.xml$")


def parse_coverage(report_path: str):
    """Parse coverage.cobertura.xml using a hardened parser and return line_rate."""
    tree = xml_parse(report_path)
    root = tree.getroot()
    line_rate = float(root.attrib.get("line-rate", "0"))
    branch_rate = float(root.attrib.get("branch-rate", "0"))
    return line_rate, branch_rate


def main():
    if len(sys.argv) < 3:
        report_path = os.environ.get("REPORT_PATH")
        threshold_str = os.environ.get("COVERAGE_THRESHOLD")
        if not report_path or not threshold_str:
            print("Usage: check-coverage.py <report> <threshold>")
            print("  or set REPORT_PATH and COVERAGE_THRESHOLD env vars.")
            sys.exit(2)
    else:
        report_path = sys.argv[1]
        threshold_str = sys.argv[2]

    if not _PATH_PATTERN.match(report_path):
        _err(f"::error::Refusing to read report with unsafe path: {report_path}")
        sys.exit(2)

    threshold = float(threshold_str) / 100.0

    try:
        line_rate, branch_rate = parse_coverage(report_path)
    except Exception as e:
        _err(f"::error::Failed to parse coverage report: {e}")
        sys.exit(1)

    line_pct = line_rate * 100
    branch_pct = branch_rate * 100
    _notice(f"::notice::Line coverage:   {line_pct:.2f}%")
    _notice(f"::notice::Branch coverage: {branch_pct:.2f}%")
    _notice(f"::notice::Threshold:       {threshold * 100:.2f}% (line)")

    if line_rate < threshold:
        _err(
            f"::error::Coverage {line_pct:.2f}% is below "
            f"threshold {threshold * 100:.2f}%"
        )
        sys.exit(1)
    _notice("Coverage gate passed.")
    sys.exit(0)


if __name__ == "__main__":
    main()
