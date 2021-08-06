# Validator Tool 123
A simple tool for controlling assets in Unity projects. The main goal is to ensure better organization and at the same time define the rules in the project. The validator checks the correct naming and placement of assets. The following types of assets are checked:

- Folders
- Prefabs
- Scripts
- Textures
- Scenes
- 3D Graphics / Models
- Sounds / AudioClips
- Materials / Shaders
- Animations / Anim. Controllers
 
Types of errors:

- Invalid asset name
- Incorrect asset location / location
- The folder does not exist
- The folder does not contain
- Invalid folder

Patterns of Naming - a section in which individual assets are assigned patterns (templates) that determine how the assets in the project must be named. Patterns to choose from:

- ExampleOfNaming
- exampleOfNaming
- example_of_naming
- example-of-naming
- Example_Of_Naming


## Root Folders
I need to have individual assets in this folder or folders. Assets can also be found in subdirectories.

Example: The root folder for textures is named MyTextures. All textures must be in this folder. Exceptions are textures that are listed in the Conditions.

## Special Folders
specific folders that are defined according to the needs of the project and that the project must contain. E.g. the Controllers, Animations, Tests, etc. folder.

## Ignore Folders
these folders and their contents are ignored by the validator during scanning. Predefined folders:

- .git
- Plugins
- Editor
- Resources
- Editor Default Resources
- Gizmos

## Project Conditions
using conditions it is possible to define the location of assets that are related or belong to a common group.

Example: GameContent \ Units \ * \ Prefabs - all folders in the Units folder must contain the Prefabs folder, which must contain at least one asset, in this case prefab.
