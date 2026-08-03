"""Blender entry point for the Memory Recycler Archive City Kit.

Example (PowerShell):
  & blender.exe --background --python Tools/Blender/create_central_archive.py -- --blender-export --preview

The implementation lives in Tools/ModelGeneration so the same geometry can be
generated and verified without requiring Blender to be installed.
"""

from pathlib import Path
import runpy


GENERATOR = Path(__file__).resolve().parents[1] / "ModelGeneration" / "generate_archive_city_kit.py"
runpy.run_path(str(GENERATOR), run_name="__main__")
