
#include "RescueSystem.h"
#include "crow.h"

#include <cstdlib>
#include <iostream>
#include <sstream>
#include <string>
#include <vector>

int main() {
    // Create the rescue system for 106 city waypoints.
    RescueSystem rescue(106);

    // Load the graph from the CSV files in the project folder.
    if (!rescue.loadGraphFromCSV("city_nodes.csv", "city_edges.csv")) {
        std::cerr << "Failed to load city graph.\n";
        return 1;
    }

    crow::SimpleApp app;

    // Endpoint: /route?source=0&destination=105
    CROW_ROUTE(app, "/route")
        .methods(crow::HTTPMethod::GET)
        ([&rescue](const crow::request& req) {
            const char* sourceText = req.url_params.get("source");
            const char* destinationText =
                req.url_params.get("destination");

            if (sourceText == nullptr || destinationText == nullptr) {
                crow::response response(400, R"({"error":"Provide source and destination"})");
                response.set_header("Content-Type", "application/json");
                return response;
            }

            char* endSource = nullptr;
            char* endDestination = nullptr;

            long source = std::strtol(sourceText, &endSource, 10);
            long destination =
                std::strtol(destinationText, &endDestination, 10);

            if (*sourceText == '\0' || *endSource != '\0' ||
                *destinationText == '\0' || *endDestination != '\0' ||
                source < 0 || source >= rescue.getTotalSectors() ||
                destination < 0 ||
                destination >= rescue.getTotalSectors()) {
                crow::response response(400, R"({"error":"Invalid sector ID"})");
                response.set_header("Content-Type", "application/json");
                return response;
            }

            std::vector<int> path = rescue.findShortestRoute(
                static_cast<int>(source),
                static_cast<int>(destination)
            );

            if (path.empty()) {
                crow::response response(404, R"({"error":"No route found"})");
                response.set_header("Content-Type", "application/json");
                return response;
            }

            int totalTime = rescue.calculateRouteTime(path);

            std::ostringstream json;
            json << "{\"source\":" << source
                 << ",\"destination\":" << destination
                 << ",\"route\":[";

            for (std::size_t i = 0; i < path.size(); ++i) {
                if (i > 0) json << ",";
                json << path[i];
            }

            json << "],\"total_time\":" << totalTime << "}";

            crow::response response(200, json.str());
            response.set_header("Content-Type", "application/json");
            return response;
        });

    std::cout << "Drone Rescue API is running at "
              << "http://127.0.0.1:8080\n";

    app.bindaddr("127.0.0.1").port(8080).run();
}
