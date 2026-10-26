#ifndef RESCUESYSTEM_H
#define RESCUESYSTEM_H

#include <string>
#include <vector>
#include <queue>
#include "Models.h"

class RescueSystem {
private:
    int total_sectors;
    std::vector<std::vector<Edge>> city_map;
    std::vector<Drone> fleet;
    std::priority_queue<Request, std::vector<Request>, RequestComparator> requests;

public:
    explicit RescueSystem(int sectors);

    // CSV files: city_nodes.csv has id,x,y,z; city_edges.csv has source,destination,travel_time.
    bool loadGraphFromCSV(const std::string& nodesFile, const std::string& edgesFile);
    void addFlightRoute(int sector_A, int sector_B, int flight_time);
    void displayMap() const;
    void displayDrones() const;

    void addDrone(int drone_id, int battery_level, int current_location, std::string status);
    Drone* findDrone(int drone_id);
    void addRequest(int request_id, int destination_sector, int priority_level);
    void displayNextRequest() const;
    void processNextRequest();
    Drone* findAvailableDrone();

    std::vector<int> findShortestRoute(int source, int destination) const;
    int calculateRouteTime(const std::vector<int>& path) const;
    void updateMissions();
    void tick();
    bool running() const;
    void updateCharging();

    int getTotalSectors() const { return total_sectors; }
};

#endif
