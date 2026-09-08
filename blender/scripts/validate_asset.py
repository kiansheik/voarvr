"""blender --background source.blend --python-exit-code 1 --python this_file.py"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from asset_common import export_objects, validate

if __name__ == "__main__":
    objects = validate(export_objects())
    print(f"VALID: {len(objects)} static mesh object(s)")
