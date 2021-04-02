# Open Rails Architecture

This document will describe the overall structure of Open Rails and how we expect different areas of the program to work together.

**Note:** In some areas, the architecture of Open Rails today is different to this design document. Such differences will be documented _in italics_. In all cases, this should be treated as a bug in the code which needs to be addressed.

## Threading model

The threading in Open Rails has two key threads working together (Render and Updater) to simulate and render the world, with a number of auxiliary threads for other functions.

- Render process [main thread]
  - Read user input
  - Swap next/current frames
  - Resume Updater
  - Render current frame
  - Wait until Updater is finished
- Updater process
  - Suspended until restarted by Render
  - Every 250ms if Loader is suspended: check for anything to load and resume Loader
  - Run simulation
  - Prepare next frame for rendering
- Loader process
  - Suspended until restarted by Updater
  - Load content for simulation and rendering
- Sound process
  - Wait 50ms
  - Update all sound outputs (volumes, 3D position, etc.)
- Watchdog process
  - Every 1s: checks above processes are making forward progress
  - If a process stops responding for more than 10s (60s for Loader), the whole application is terminated with an error containing the hung process' stack trace
- Web Server process
  - Handle all web and API requests

## Object model

Each entry in this tree is a class accessible from its parent via the property in square brackets. Those in _italics_ deviate from the desired architecture as described on each item.

- `Simulator`
  - `Train` (collection) [`Trains`]
    - `TrainCar` (collection) [`Cars`]
      - **Physics simulation**
      - `BrakeSystem` [`BrakeSystem`]
      - `Coupler` (collection) [`Couplers`] - _currently `MSTSCoupling` (collection) on `MSTSWagon`_
      - `TractionSystem` [`TractionSystem`] - _currently does not exist_
      - `TrainCarController` [`TrainCarController`] - _currently does not exist_
      - `WheelAxle` (collection) [`WheelAxles`]
      - **Visual simulation**
      - `CabView` (collection) [`CabViewList`] - _currently on `MSTSLocomotive`_
      - _`CabView3D` [`CabView3D`] - merge into `CabView` (collection)_
      - `IntakePoint` (collection) [`IntakePointList`] - _currently on `MSTSWagon`_
      - `Pantograph` (collection) [`Pantographs`] - _currently on `MSTSWagon`_
      - `ParticleEmitterData` (collection) [`EffectData`] - _currently on `MSTSWagon`_
      - `TrainCarPart` (collection) [`Parts`]
    - `TrainController` [`TrainController`] - _currently does not exist_
  - `UserSettings` [`Settings`]
  - `Weather` [`Weather`]

The new class `TractionSystem` will contain the following existing classes:

- `DieselEngine` (collection) [`DieselEngines`] - _currently on `MSTSDieselLocomotive`_
- `GearBox` [`GearBox`] - _currently on `MSTSDieselLocomotive` and `DieselEngine`_
- `ScriptedBrakeController` [`TrainBrakeController`, `EngineBrakeController`, `BrakemanBrakeController`] - _currently on `MSTSLocomotive`_
- `ScriptedElectricPowerSupply` [`PowerSupply`] - _currently on `MSTSElectricLocomotive`_
  - `ScriptedCircuitBreaker` [`CircuitBreaker`]
- `ScriptedTrainControlSystem` [`TrainControlSystem`] - _currently on `MSTSLocomotive`_

## Class hierarchy

Each entry in this tree is a class which inherits from its parent. Those in _italics_ deviate from the desired architecture as described on each item.

- `BrakeSystem`
  - `MSTSBrakeSystem`
    - `AirSinglePipe`
      - `AirTwinPipe`
        - `EPBrakeSystem`
    - `ManualBraking`
    - `VacuumSinglePipe`
      - `straightVacuumSinglePipe`
- `Train`
  - _`AITrain` - merge into `Train` and `AITrainController`_
    - _`TTTrain` - merge into `Train`_
- `TrainCar`
  - _`MSTSWagon` - merge into `TrainCar`_
    - _`MSTSLocomotive` - merge into `TrainCar` and `TractionSystem` and `TrainCarController`_
      - _`MSTSDieselLocomotive` - merge into `TrainCar` and `TractionSystem` and `TrainCarController`_
      - _`MSTSElectricLocomotive` - merge into `TrainCar` and `TractionSystem` and `TrainCarController`_
      - _`MSTSSteamLocomotive` - merge into `TrainCar` and `TractionSystem` and `TrainCarController`_
- `TrainCarController` (abstract) - _currently does not exist_
  - `LocalTrainCarController` - _currently does not exist, will provide local user input control_
  - `RemoteTrainCarController` - _currently does not exist, will provide remote/networked control_
- `TrainController` (abstract) - _currently does not exist_
  - `AITrainController` - _currently does not exist, will provide train-level AI control_
  - `PerCarTrainController` - _currently does not exist, will delegate simulation to `TrainCarController`_
