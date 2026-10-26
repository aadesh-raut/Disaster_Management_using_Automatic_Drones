#include "RescueSystem.h"

#include <algorithm>
#include <climits>
#include <fstream>
#include <iostream>
#include <queue>
#include <sstream>
#include <stdexcept>

using namespace std;

RescueSystem::RescueSystem(int sectors)
    : total_sectors(sectors), city_map(sectors) {
    if (sectors <= 0) {
        throw invalid_argument("The number of sectors must be positive.");
    }
}

bool RescueSystem::loadGraphFromCSV(const string& nodesFile, const string& edgesFile) {
    ifstream nodes(nodesFile);
    if (!nodes.is_open()) {
        cerr << "Error: could not open node CSV: " << nodesFile << "\n";
        return false;
    }

    string line;
    if (!getline(nodes, line)) {
        cerr << "Error: node CSV is empty.\n";
        return false;
    }

    vector<bool> foundNode(total_sectors, false);
    int nodeCount = 0;
    while (getline(nodes, line)) {
        if (line.empty()) continue;
        // Strip UTF-8 BOM if present at the beginning of the first field.
        if (line.size() >= 3 &&
            static_cast<unsigned char>(line[0]) == 0xEF &&
            static_cast<unsigned char>(line[1]) == 0xBB &&
            static_cast<unsigned char>(line[2]) == 0xBF) {
            line.erase(0, 3);
        }

        string idText, xText, yText, zText;
        stringstream row(line);
        if (!getline(row, idText, ',') || !getline(row, xText, ',') ||
            !getline(row, yText, ',') || !getline(row, zText, ',')) {
            // A final field without a trailing comma is still read by getline.
            stringstream retry(line);
            vector<string> fields;
            string field;
            while (getline(retry, field, ',')) fields.push_back(field);
            if (fields.size() < 4) {
                cerr << "Error: malformed node row: " << line << "\n";
                return false;
            }
            idText = fields[0];
        }

        try {
            size_t used = 0;
            int id = stoi(idText, &used);
            if (used != idText.size() || id < 0 || id >= total_sectors) {
                cerr << "Error: node ID out of range: " << idText << "\n";
                return false;
            }
            if (foundNode[id]) {
                cerr << "Error: duplicate node ID: " << id << "\n";
                return false;
            }
            foundNode[id] = true;
            ++nodeCount;
        } catch (const exception&) {
            cerr << "Error: invalid node ID: " << idText << "\n";
            return false;
        }
    }

    for (int id = 0; id < total_sectors; ++id) {
        if (!foundNode[id]) {
            cerr << "Error: node CSV is missing ID " << id << "\n";
            return false;
        }
    }

    ifstream edges(edgesFile);
    if (!edges.is_open()) {
        cerr << "Error: could not open edge CSV: " << edgesFile << "\n";
        return false;
    }
    if (!getline(edges, line)) {
        cerr << "Error: edge CSV is empty.\n";
        return false;
    }

    // Load into a temporary graph so a bad CSV doesn't leave a partial graph.
    vector<vector<Edge>> loadedGraph(total_sectors);
    int edgeCount = 0;
    while (getline(edges, line)) {
        if (line.empty()) continue;
        string sourceText, destinationText, timeText;
        stringstream row(line);
        if (!getline(row, sourceText, ',') ||
            !getline(row, destinationText, ',') ||
            !getline(row, timeText, ',')) {
            cerr << "Error: malformed edge row: " << line << "\n";
            return false;
        }

        try {
            int source = stoi(sourceText);
            int destination = stoi(destinationText);
            int travelTime = stoi(timeText);
            if (source < 0 || source >= total_sectors ||
                destination < 0 || destination >= total_sectors) {
                cerr << "Error: edge references an unknown node: " << line << "\n";
                return false;
            }
            if (source == destination || travelTime <= 0) {
                cerr << "Error: edge must connect two different nodes with positive travel_time: "
                     << line << "\n";
                return false;
            }
            loadedGraph[source].push_back({destination, travelTime});
            loadedGraph[destination].push_back({source, travelTime});
            ++edgeCount;
        } catch (const exception&) {
            cerr << "Error: invalid edge row: " << line << "\n";
            return false;
        }
    }

    city_map.swap(loadedGraph);
    cout << "Loaded " << nodeCount << " nodes and " << edgeCount
         << " bidirectional edges from CSV files.\n";
    return true;
}

void RescueSystem::addFlightRoute(int sector_A, int sector_B, int flight_time) {
    if (sector_A < 0 || sector_A >= total_sectors ||
        sector_B < 0 || sector_B >= total_sectors ||
        sector_A == sector_B || flight_time <= 0) {
        cerr << "Warning: ignored invalid flight route.\n";
        return;
    }
    city_map[sector_A].push_back({sector_B, flight_time});
    city_map[sector_B].push_back({sector_A, flight_time});
}

void RescueSystem::displayMap() const {
    cout << "\n===== CITY MAP =====\n";
    for (int i = 0; i < total_sectors; ++i) {
        cout << "Sector " << i << " -> ";
        for (const auto& edge : city_map[i]) {
            cout << "(" << edge.target_sector << ", " << edge.flight_time << " min) ";
        }
        cout << '\n';
    }
}

static bool droneIdLess(const Drone& a, const Drone& b) {
    return a.drone_id < b.drone_id;
}

void RescueSystem::addDrone(int drone_id, int battery_level, int current_location, string status) {
    if (current_location < 0 || current_location >= total_sectors) {
        cout << "Invalid drone starting sector. Use an ID from 0 to "
             << total_sectors - 1 << ".\n";
        return;
    }
    if (battery_level < 0 || battery_level > 100) {
        cout << "Battery level must be between 0 and 100.\n";
        return;
    }
    fleet.emplace_back(drone_id, battery_level, current_location, status);
    sort(fleet.begin(), fleet.end(), droneIdLess);
}

void RescueSystem::displayDrones() const {
    cout << "\n===== DRONE FLEET =====\n";
    for (const auto& drone : fleet) {
        cout << "Drone ID: " << drone.drone_id
             << " | Battery: " << drone.battery_level << "%"
             << " | Location: Sector " << drone.current_location
             << " | Status: " << drone.status
             << " | mission_destination: " << drone.mission_destination << '\n';
    }
}

Drone* RescueSystem::findDrone(int drone_id) {
    auto it = lower_bound(fleet.begin(), fleet.end(), drone_id,
        [](const Drone& drone, int id) { return drone.drone_id < id; });
    if (it == fleet.end() || it->drone_id != drone_id) {
        cout << "Drone not found.\n";
        return nullptr;
    }
    return &(*it);
}

void RescueSystem::addRequest(int request_id, int destination_sector, int priority_level) {
    if (destination_sector < 0 || destination_sector >= total_sectors) {
        cout << "Invalid destination sector. Use an ID from 0 to "
             << total_sectors - 1 << ".\n";
        return;
    }
    Request request;
    request.request_id = request_id;
    request.destination_sector = destination_sector;
    request.priority_level = priority_level;
    requests.push(request);
}

void RescueSystem::displayNextRequest() const {
    if (requests.empty()) {
        cout << "No emergency requests.\n";
        return;
    }
    const Request& current = requests.top();
    cout << "\n===== NEXT EMERGENCY =====\n"
         << "Request ID: " << current.request_id << '\n'
         << "Destination: Sector " << current.destination_sector << '\n'
         << "Priority: " << current.priority_level << '\n';
}

void RescueSystem::processNextRequest() {
    if (requests.empty()) {
        cout << "No emergency requests to process.\n";
        return;
    }

    const Request current = requests.top();
    Drone* drone = findAvailableDrone();
    if (drone == nullptr) {
        cout << "No available drone to process the request.\n";
        return;
    }

    vector<int> path = findShortestRoute(drone->current_location, current.destination_sector);
    if (path.empty()) {
        cout << "No route exists from Sector " << drone->current_location
             << " to Sector " << current.destination_sector
             << ". Request " << current.request_id << " remains queued.\n";
        // Avoid an infinite simulation loop when this request cannot be served.
        requests.pop();
        return;
    }

    int time = calculateRouteTime(path);
    if (drone->battery_level < 35 || drone->battery_level < time * 2) {
        cout << "Drone " << drone->drone_id
             << " has insufficient battery and is being sent for charging.\n";
        drone->status = "charging";
        return;
    }

    drone->status = "On Mission";
    drone->remaining_time = time * 2;
    drone->battery_level -= time * 2;
    drone->mission_destination = current.destination_sector;
    cout << "Drone " << drone->drone_id << " assigned to request "
         << current.request_id << ". Route: ";
    for (size_t i = 0; i < path.size(); ++i) {
        if (i) cout << " -> ";
        cout << path[i];
    }
    cout << " | Estimated route cost: " << time << '\n';
    requests.pop();
}

Drone* RescueSystem::findAvailableDrone() {
    for (auto& drone : fleet) {
        if (drone.status == "available") return &drone;
    }
    return nullptr;
}

vector<int> RescueSystem::findShortestRoute(int source, int destination) const {
    if (source < 0 || source >= total_sectors ||
        destination < 0 || destination >= total_sectors) return {};

    vector<int> distance(total_sectors, INT_MAX);
    vector<int> parent(total_sectors, -1);
    priority_queue<pair<int, int>, vector<pair<int, int>>, greater<pair<int, int>>> pq;

    distance[source] = 0;
    pq.push({0, source});

    while (!pq.empty()) {
        const int currentDistance = pq.top().first;
        const int u = pq.top().second;
        pq.pop();
        if (currentDistance != distance[u]) continue;
        if (u == destination) break;

        for (const auto& edge : city_map[u]) {
            if (distance[u] != INT_MAX &&
                distance[edge.target_sector] > distance[u] + edge.flight_time) {
                distance[edge.target_sector] = distance[u] + edge.flight_time;
                parent[edge.target_sector] = u;
                pq.push({distance[edge.target_sector], edge.target_sector});
            }
        }
    }

    if (distance[destination] == INT_MAX) return {};
    vector<int> path;
    for (int current = destination; current != -1; current = parent[current]) {
        path.push_back(current);
    }
    reverse(path.begin(), path.end());
    return path;
}

int RescueSystem::calculateRouteTime(const vector<int>& path) const {
    int totalTime = 0;
    for (size_t i = 0; i + 1 < path.size(); ++i) {
        int from = path[i];
        int to = path[i + 1];
        bool found = false;
        for (const auto& edge : city_map[from]) {
            if (edge.target_sector == to) {
                totalTime += edge.flight_time;
                found = true;
                break;
            }
        }
        if (!found) return INT_MAX;
    }
    return totalTime;
}

void RescueSystem::updateMissions() {
    for (auto& drone : fleet) {
        if (drone.status != "On Mission") continue;
        if (drone.remaining_time > 0) --drone.remaining_time;
        cout << "Drone " << drone.drone_id << " | Remaining time: "
             << drone.remaining_time << " minutes\n";
        if (drone.remaining_time == 0) {
            drone.current_location = drone.mission_destination;
            drone.mission_destination = -1;
            if (drone.battery_level < 35) {
                drone.status = "charging";
                cout << "Battery below 35%. Drone sent for charging.\n";
            } else {
                drone.status = "available";
                cout << "Drone " << drone.drone_id << " is now available at Sector "
                     << drone.current_location << ".\n";
            }
        }
    }
}

void RescueSystem::tick() {
    updateMissions();
    updateCharging();
    while (!requests.empty() && findAvailableDrone() != nullptr) {
        const size_t before = requests.size();
        processNextRequest();
        // A request can remain queued when the drone is charging or unavailable.
        if (requests.size() == before) break;
    }
}

bool RescueSystem::running() const {
    if (!requests.empty()) return true;
    for (const auto& drone : fleet) {
        if (drone.status == "On Mission" || drone.status == "charging") return true;
    }
    return false;
}

void RescueSystem::updateCharging() {
    for (auto& drone : fleet) {
        if (drone.status != "charging") continue;
        drone.battery_level = min(100, drone.battery_level + 5);
        if (drone.battery_level >= 100) {
            drone.status = "available";
            cout << "Drone " << drone.drone_id
                 << " fully charged and is now available.\n";
        } else {
            cout << "Drone " << drone.drone_id << " charging: "
                 << drone.battery_level << "%\n";
        }
    }
}
