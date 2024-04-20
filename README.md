Unity Tools for working with Data objects via CSV files in Unity. This is very useful when defining large numbers of data objects as you 
can import the raw data into a spreadsheet, work on the data and re-import into Unity.

# Instalaltion

CHeckout the code into your project, currently we are only setup for checking out as a submodule, 
but it would be peerfectly possible to make this a package (patches welcome).

```
git submodule add https://github.com/TheWizardsCode/UnityDataManager.git path/to/submodule
git submodule init
git submodule update
```

# Usage

* Tools -> Wizards Code -> Data Manager
* Drag the ScriptableObject you want to work with into the `Scriptable Object Class` field
* Check the data directory is set correctly
* Click `Export Data to CSV`
* Edit in your favourite CSV editor
* Click `Import Data from CSV`

All changes made in the CSV will be reflected in the project.
