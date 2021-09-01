# Validator Tool
123 A simple tool for controlling assets in Unity projects. The main goal is to ensure better organization and at the same time define the rules in the project. The validator checks the correct naming and placement of assets. 

:white_check_mark: <b>The following types of assets are checked:</b>
- Folders
- Prefabs
- Scripts
- Textures
- Scenes
- 3D Graphics / Models
- Sounds / AudioClips
- Materials / Shaders
- Animations / Anim. Controllers
 
:x: <b>Types of errors:</b>

- Invalid asset name
- Incorrect asset location / location
- The folder does not exist
- The folder does not contain
- Invalid folder

:scroll: <b>Patterns of Naming</b> - a section in which individual assets are assigned patterns (templates) that determine how the assets in the project must be named. Patterns to choose from:

- ExampleOfNaming
- exampleOfNaming
- example_of_naming
- example-of-naming
- Example_Of_Naming


## :file_folder: Root Folders
A folder or folders in which assets of a specific type must be placed. Assets can be placed in subdirectories. Exceptions are assets that are listed in the Conditions.

<i>Example</i>: The root folder for textures is named Textures. All textures must be in this folder. Exceptions are textures that are listed in the Conditions.

## :file_folder: Special Folders
Specific folders that are defined according to the needs of the project and that the project must contain.

<i>Example</i>: Controllers, Animations, Tests, etc.

## :file_folder: Ignore Folders
These folders and their contents are ignored by the validator during scanning. Predefined folders:

- .git
- Plugins
- Editor
- Resources
- Editor Default Resources
- Gizmos

## :clipboard: Project Conditions
Using conditions it is possible to define the location of assets that are related or belong to a common group.

<i>Example</i>: GameContent\Units\*\ Prefabs - all folders in the Units folder must contain the Prefabs folder, which must contain at least one asset, in this case prefab.

## Basic Layout
![Validator Basic Layout](http://dev.unobex.eu/images/ValidatorBasicLayout.png)
