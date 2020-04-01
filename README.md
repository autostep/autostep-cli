# autostep-cli

Command Line Interface for [AutoStep BDD](https://github.com/autostep/AutoStep).

Some example usages:

**Getting Help**

```batch
> autostep-cli -h

Usage:
  autostep-cli [options] [command]

Options:
  -?, -h, --help    Show help and usage information
  --version         Show version information

Commands:
  run      Execute tests.
  build    Compile and Link tests.

```

**Build**

```batch

> autostep-cli build -h

build:
  Compile and Link tests.

Usage:
  autostep-cli build [options]

Options:
  -d, --directory <d>                                                    Provide the base directory for the autostep project.
  -c, --config <c>                                                       Specify the autostep configuration file.
  -v, --verbose <Critical|Debug|Error|Information|None|Trace|Warning>    Indicates the verbosity level.
  -?, -h, --help                                                         Show help and usage information

> autostep-cli build

MyInteraction.asi(9,30,9,40): Error ASC30013: The specified interaction method 'click' is not available. It should be either be declared with an associated expression, or with 'needs-defining'. E.g. 'click: method()' or 'click: needs-defining'.
MyInteraction.asi(12,30,12,38): Error ASC30013: The specified interaction method 'visible' is not available. It should be either be declared with an associated expression, or with 'needs-defining'. E.g. 'visible: method()' or 'visible: needs-defining'.
MyInteraction.asi(20,24,20,53): Error ASC30008: The specified interaction method 'select' is not available. It must be declared with an associated expression, e.g. 'select: method()'.
MyInteraction.asi(26,24,26,53): Error ASC30008: The specified interaction method 'select' is not available. It must be declared with an associated expression, e.g. 'select: method()'.
MyInteraction.asi(31,9,31,16): Error ASC30008: The specified interaction method 'select' is not available. It must be declared with an associated expression, e.g. 'select: method()'.

```