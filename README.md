# Bancada
Bancada is a collaborative geospatial multi-display system. It was developed (as part of my MSc thesis) to explore how geospatial information can be visualized and manipulated across multiple displays -- efficiently supporting collaborative activities. As result, multiple users can explore different detailed visualizations (lenses) using tablets, while a large display provides overview.

### Modes
When a user opens Bancada, a menu will appear to choose which mode will be executed on that device. There are three modes:
* Overview - provides only overview map and detailed views. Doesn't allow direct interaction (output-only).
* SingleLens - provides a specific lens (layer), defined in-code.
* MultipleLenses - allow users to explore any lens through a menu.

### Interactions
Users interact with lenses using common touch gestures: drag to pan the map, and pinch to change its zoom scale.

### Technical Information
Bancada was implemented in C#, using WPF. Devices communicate with each other using the multi-surface environment toolkit [SoD](http://sodtoolkit.com). Last, maps provided by the [ESRI Canada Community Map Program](http://maps.esri.ca) are rendered using [ArcGIS SDK for C#](https://developers.arcgis.com/net/).

For more information, please refer to this [paper](https://dl.dropboxusercontent.com/u/961825/ase/Bancada-CMiS-ITS-2014.pdf).

## System requirements
* Windows 7+
* [SoD Locator](https://github.com/ase-lab/SoD_Locator_SS)

## Installation
Currently, you have to build and run the system using Visual Studio 13+. A bundle with an executable file is under development.

## Running
For maximum benefit of the system, I recommended to run the overview on a computer connected to a large HD display (4K TV, wall display, or a digital tabletop); for the other modes, Microsoft Surface Pro 3 are excellent.

## Backlog
- [ ] Expand documentation
- [ ] Video
- [ ] Standalone executable file
- [ ] Runtime configuration
- [ ] Lib providing overview elements (insets/shadows/lenses)
