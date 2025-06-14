
# To-Do's

- add dynamics content for commit message and update information tooltip accordingly
- allow for dynamic content in the filenames
- save and import config file / alternatively create multiple profiles per connection for easier access
- add possibility to transfer data
- allow execution of powershell at any point in the execution flow
- allow for custom text size in solution list
- add option "Abort on Error"


## Added in Release 2025.06.<mark>XX</mark>

## Added in Release 2025.06.11

- better statuses on worker panel
- storing last import / export times for reference
- installing as update (not upgrade) automatically if solution is not yet installed.

## Added in Release 2025.06.02

- added file location wizzard to allow for easier file location definition
- made splitter more visible

## Added in Release 2025.03.19

- added timeout setting used for all used connections
- fixed multiple minor bugs
	- GIT root repo was not always found correctly
	- some settings were loaded incorrectly
- miscellaneous other improvements

## Added in Release 2025.03.01
 
- fixed bug where splitter-pos restore would fail
- better / cleaner logging

## Added in Release 2025.02.25

- show current branch and allow changing
- added option to import solutions with Upgrade / Update
- Saving splitter position in settings now

## Added in Release 2025.02.19

- added rich text logging
- allow for multiple target envionments
- allow for sorting of all text columns in the solution list
- export solution version as *.csv file
- grab import error xml and format it properly in logs
- unify logging so it is formatted the same everywhere
- add *.json file to solution storage with exported version information.
 
## Added in Release 2024.12.11

- load installed versions when connecting to target and visualize version difference
- added tooltips to sortable check list
- options for showing/hiding the column for friendly solution names 
- internal code improvements
- UI is now disabled when execution is running
- logging execution time of process steps

## Added in Release 2024.11.29

- Better internal code for allowing for defining column width as percent and pixels
- Better usability by noting that a row need to be selected for adding files
- New warning for not yet exported or outdated files
- Added warning if certain file names were not defined before Execution

## Added in Release 2024.11.11

- Bugfixes in saving the settings which removed / destroyed already defined configs

## Added in Release 2024.11.10

- added multiple columns
- added sorting by name functionality
- add option for showing logical names of solutions
- show icons for the solutions (visualization of file status)
- made it moire visible to what target one is connected

## Added in Release 2024.10.28

- better list for solutions
- allow for sorting solutions for import order
- added buttons for checking- / unchecking all solutions in the list
- added publish all for target

## Added in Release 2024.10.21

- make commit message mandatory
- check version number format before allowing export
- improve target environment visability (disabling the connect button after a target is connected)

## Added in Release 2024.10.14

- commit message
- import options (activate workflows / plugin steps, overwrite)
