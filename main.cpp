#include "RescueSystem.h"

#include <chrono>
#include <cctype>
#include <iostream>
#include <limits>
#include <string>
#include <thread>

using namespace std;

int main(int argc, char* argv[]) {
    // Default paths are relative to the folder from which you run the program.
    const string nodesFile = (argc > 1) ? argv[1] : "city_nodes.csv";
    const string edgesFile = (argc > 2) ? argv[2] : "city_edges.csv";

    // This city currently has IDs 0 through 105: 106 nodes total.
    RescueSystem system(106);
    if (!system.loadGraphFromCSV(nodesFile, edgesFile)) {
        cerr << "\nCould not load the city graph. Check that both CSV files are "
             << "in the current working directory, or pass their paths as arguments.\n";
        return 1;
    }

    // Demo fleet. All current locations are valid node IDs.
    system.addDrone(101, 90, 0, "available");
    system.addDrone(102, 75, 0, "available");
    system.addDrone(103, 40, 0, "charging");
    system.addDrone(105, 20, 0, "charging");
    system.addDrone(109, 80, 0, "available");
    system.addDrone(111, 86, 0, "available");

    // Demo requests; destinations use numeric Sector IDs.
    system.addRequest(1, 4, 2);
    system.addRequest(2, 37, 5);
    system.addRequest(3, 52, 3);
    system.addRequest(4, 101, 1);
    system.addRequest(5, 85, 5);
    system.addRequest(6, 104, 5);

    while (true) {
        cout << "\n========== DRONE RESCUE SYSTEM ==========\n"
             << "1. Display drones\n"
             << "2. Display next request\n"
             << "3. Add drone\n"
             << "4. Add request\n"
             << "5. Display map\n"
             << "6. Run simulation\n"
             << "7. Test shortest route\n"
             << "8. Exit\n"
             << "Enter your option: ";

        int option;
        if (!(cin >> option)) {
            cin.clear();
            cin.ignore(numeric_limits<streamsize>::max(), '\n');
            cout << "Please enter a number from 1 to 8.\n";
            continue;
        }

        if (option == 1) {
            system.displayDrones();
        } else if (option == 2) {
            system.displayNextRequest();
        } else if (option == 3) {
            int id, battery, location;
            string status;
            cout << "Enter drone ID: ";
            cin >> id;
            cout << "Enter battery level (0-100): ";
            cin >> battery;
            cout << "Enter starting sector ID (0-105): ";
            cin >> location;
            cout << "Enter status (available/charging): ";
            cin >> status;
            for (char& ch : status) ch = static_cast<char>(tolower(static_cast<unsigned char>(ch)));
            system.addDrone(id, battery, location, status);
        } else if (option == 4) {
            int id, destination, priority;
            cout << "Enter request ID: ";
            cin >> id;
            cout << "Enter destination sector ID (0-105): ";
            cin >> destination;
            cout << "Enter priority level: ";
            cin >> priority;
            system.addRequest(id, destination, priority);
        } else if (option == 5) {
            system.displayMap();
        } else if (option == 6) {
            cout << "Running simulation. Press Ctrl+C to stop.\n";
            int tickNumber = 1;
            while (system.running()) {
                cout << "\n========== TICK " << tickNumber++ << " ==========\n";
                system.tick();
                this_thread::sleep_for(chrono::seconds(2));
            }
            cout << "Simulation finished.\n";
        } else if (option == 7) {
            int source, destination;
            cout << "Enter source sector ID (0-105): ";
            cin >> source;
            cout << "Enter destination sector ID (0-105): ";
            cin >> destination;
            const auto path = system.findShortestRoute(source, destination);
            if (path.empty()) {
                cout << "No route found, or one of the sector IDs is invalid.\n";
            } else {
                cout << "Shortest route: ";
                for (size_t i = 0; i < path.size(); ++i) {
                    if (i) cout << " -> ";
                    cout << path[i];
                }
                cout << "\nTotal travel cost: " << system.calculateRouteTime(path) << '\n';
            }
        } else if (option == 8) {
            cout << "Exiting.\n";
            return 0;
        } else {
            cout << "Invalid option. Please choose 1-8.\n";
        }
    }
}
