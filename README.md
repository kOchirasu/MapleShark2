MapleShark2
=========

## Scripting

Scripting uses IronPython 2.7.11 https://github.com/IronLanguages/ironpython2

See [script_api.py](Resources/script_api.py) for more details.

`sys.path` is set to use the `Scripts/` directory as well as the corresponding `LOCALE/VERSION/`
directory to search for modules. Modules included in a subdirectory of these path directories
will need an `__init__.py` file to be detected. Any changes to modules in these paths will cause
the `ScriptEngine` to be reloaded so that new changes can be used.

- `Scripts/`: Version agnostic functions to be shared by all scripts.
- `LOCALE/VERSION`: Version specific functions to be used only by containing directory.

`script_api.py` and `common.py` are imported at `ScriptEngine` construction for quicker responsiveness when using them.