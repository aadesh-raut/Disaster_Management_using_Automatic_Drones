# Disaster Management Using Automatic Drones

## Overview

Disaster Management Using Automatic Drones is a 3D simulation project developed using Unity and C++. The project aims to demonstrate how drones can be used to respond to emergencies in a city by identifying incident locations and calculating suitable routes.

The system uses a virtual city environment containing buildings, roads, and predefined waypoints. When an incident is generated, the backend calculates the shortest route to the incident location using Dijkstra's algorithm. The drone fleet manager then selects an available drone based on its battery level and assigns it the calculated route.

The project combines a Unity-based simulation with a C++ backend to demonstrate automated drone dispatch and route planning.

## Features

- **3D City Simulation:** A virtual city environment created in Unity.
- **Incident Generation:** Random rooftop incident locations generated using raycasting.
- **Shortest Path Calculation:** Dijkstra's algorithm is used to calculate routes between city sectors.
- **Automated Drone Dispatch:** The system selects an eligible drone for an incident.
- **Battery Management:** Drones have battery levels, charging rates, and reserve requirements.
- **Return Route Planning:** The system calculates a return route after reaching the incident location.
- **Multiple Drone Management:** Supports a fleet of drones operating within the simulated city.
- **Backend Communication:** Unity communicates with the C++ backend through an HTTP API.

## Technology Stack

| Technology | Purpose |
|---|---|
| Unity 6 | 3D city and drone simulation |
| C# | Unity scripts and drone management |
| C++17 | Backend logic and pathfinding |
| Crow | C++ HTTP server framework |
| Dijkstra's Algorithm | Shortest path calculation |
| REST API | Communication between Unity and C++ |
| JSON | Data exchange between frontend and backend |
| CSV | Storage of city nodes and edges |
| CMake | C++ project build system |
| vcpkg | C++ dependency management |
| Git and GitHub | Version control |

## System Architecture

The project consists of three main parts:

### 1. Unity Simulation

Unity is responsible for the visual representation and simulation of the system.

It handles:
- City environment and buildings.
- Drone models and movement.
- Rooftop incident generation.
- Drone fleet management.
- Battery monitoring and charging.

### 2. C++ Backend

The C++ backend handles route calculations and graph-related operations.

It uses:
- City nodes and edges to represent the city as a graph.
- Dijkstra's algorithm to calculate the shortest route.
- Travel-time values to determine route costs.

### 3. Communication Layer

The Crow framework provides an HTTP API server that connects Unity with the C++ backend.

Unity sends the source and destination sector IDs through an HTTP GET request. The backend calculates the route and returns the result in JSON format.

## How the System Works

1. The Unity application loads the virtual city and initializes the drone fleet.
2. The user presses the Space key to generate a new incident.
3. The incident detector identifies a suitable rooftop location.
4. The nearest city waypoint is selected as the destination sector.
5. Unity sends a route request to the C++ backend.
6. The backend calculates the outbound and return routes using Dijkstra's algorithm.
7. The fleet manager checks drone availability and battery requirements.
8. An eligible drone is selected and assigned the mission.
9. The drone travels towards the incident location.
10. After completing the mission, the drone follows its return route.

## Project Structure

```text
City/
│
├── Assets/                  # Unity assets, scenes and scripts
├── Packages/                # Unity package dependencies
├── ProjectSettings/         # Unity project configuration
│
├── main.cpp                 # Terminal-based simulation
├── api_server.cpp           # HTTP API server
├── RescueSystem.cpp         # Graph and route calculation logic
├── RescueSystem.h           # Rescue system declarations
├── Models.h                 # Data structures
│
├── city_nodes.csv            # City waypoint information
├── city_edges.csv            # Connections between waypoints
│
├── CMakeLists.txt            # CMake build configuration
├── vcpkg.json                # C++ dependency manifest
└── README.md
```

## Installation and Setup

### Prerequisites

Install the following software:

- Unity 6
- Visual Studio Code or another code editor
- Git
- CMake 3.20 or later
- A C++17-compatible compiler
- vcpkg

### 1. Clone the Repository

```bash
git clone https://github.com/aadesh-raut/Disaster_Management_using_Automatic_Drones.git
```

Navigate into the project directory:

```bash
cd Disaster_Management_using_Automatic_Drones
```

### 2. Set Up vcpkg

The project uses vcpkg to manage C++ dependencies.

**macOS:**

```bash
git clone https://github.com/microsoft/vcpkg.git ~/vcpkg
cd ~/vcpkg
./bootstrap-vcpkg.sh
```

**Windows (PowerShell):**

```powershell
git clone https://github.com/microsoft/vcpkg.git C:\vcpkg
cd C:\vcpkg
.\bootstrap-vcpkg.bat
```

For Windows, set the vcpkg location:

```powershell
$env:VCPKG_ROOT = "C:\vcpkg"
```

If you already have vcpkg installed, you can use your existing installation.

### 3. Build the C++ Backend

Open a terminal in the project root directory.

**macOS:**

```bash
cmake -S . -B build-vcpkg \
  -DCMAKE_TOOLCHAIN_FILE="$HOME/vcpkg/scripts/buildsystems/vcpkg.cmake"
```

Build the project:

```bash
cmake --build build-vcpkg
```

**Windows (PowerShell):**

```powershell
cmake -S . -B build-vcpkg `
  -DCMAKE_TOOLCHAIN_FILE="$env:VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake"
```

Build the project:

```powershell
cmake --build build-vcpkg --config Release
```

The build process generates two executables:

- `drone_rescue` – Terminal-based simulation.
- `api_server` – HTTP server used by Unity.

### 4. Start the Backend Server

Make sure you are in the project root directory.

**macOS:**

```bash
./build-vcpkg/api_server
```

**Windows (PowerShell):**

```powershell
.\build-vcpkg\Release\api_server.exe
```

The API server runs on:

```text
http://127.0.0.1:8080
```

Keep the terminal running while using the Unity application.

### 5. Test the API

Open another terminal and execute:

```bash
curl "http://127.0.0.1:8080/route?source=0&destination=105"
```

Example response:

```json
{
  "source": 0,
  "destination": 105,
  "route": [0, 4, 51, 105],
  "total_time": 12
}
```

The returned route and travel time depend on the city graph data.

### 6. Run the Unity Project

1. Open Unity Hub.
2. Select **Add Project from Disk**.
3. Select the cloned project folder.
4. Open the project using a compatible Unity 6 version.
5. Make sure the C++ API server is running.
6. Open the main city scene.
7. Press Play to start the simulation.
8. Press Space in the Game view to generate an incident and initiate drone dispatch.

## API Endpoint

| Method | Endpoint | Description |
|---|---|---|
| GET | `/route?source={id}&destination={id}` | Calculates the shortest route between two sectors |

### Example Request

```text
http://127.0.0.1:8080/route?source=0&destination=105
```

### Example Parameters

- `source`: Starting sector ID.
- `destination`: Target sector ID.

### Response

The API returns:
- Source sector.
- Destination sector.
- Calculated route.
- Total travel time.

## Important Notes

- The backend server must be running before starting the Unity simulation.
- Keep `city_nodes.csv` and `city_edges.csv` in the project root directory because the backend loads them using relative paths.
- The API currently uses `127.0.0.1` and port `8080`.
- The system is a software-based drone simulation and does not directly control physical drones.
- Do not commit generated build folders or Unity cache folders to GitHub.

## Future Improvements

- Integration with real-time weather information.
- More advanced drone collision avoidance.
- Improved incident prioritization.
- Dynamic route recalculation.
- More realistic drone physics and animations.
- Integration with real-world drone hardware.

## Contributors

Developed as a disaster management and automated drone simulation project.

## License

No license has been specified for this repository yet. Please check the repository's licensing terms before reusing or distributing its code and assets.