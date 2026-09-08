"""Host-side regression tests for failure-sensitive test result handling."""
from pathlib import Path
import tempfile
import unittest
from unity import check_results


class ResultTests(unittest.TestCase):
    def check_xml(self, text):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "results.xml"
            path.write_text(text)
            return check_results(path)

    def test_passed_suite(self):
        self.assertEqual(self.check_xml('<test-run result="Passed"><test-case result="Passed"/></test-run>'), 1)

    def test_empty_failed_or_skipped_suite_is_failure(self):
        for text in ['<test-run result="Passed"/>', '<test-run result="Failed"><test-case result="Failed"/></test-run>',
                     '<test-run result="Passed"><test-case result="Skipped"/></test-run>']:
            with self.subTest(text=text), self.assertRaises(ValueError):
                self.check_xml(text)


if __name__ == "__main__":
    unittest.main()
