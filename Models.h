#ifndef MODELS_H
#define MODELS_H

#include <string>
#include <vector>

using namespace std;


struct Edge {
    int target_sector;
    int flight_time;      
};



struct Drone {
    int drone_id;
    int battery_level;     
    int remaining_time;     
    int current_location;
    string status; 
    int mission_destination;
    
    Drone(int id,int battery,int location,string status){
        this->drone_id=id;
        this->battery_level=battery;
        this->current_location=location;
        this->status=status;
        this->remaining_time=0;
        this->mission_destination=-1;
    }
    


};






struct Request {
    int request_id;
    int destination_sector;
    int priority_level;    };

struct RequestComparator
{
    // Lower integer = Higher priority (Priority 1 is processed before Priority 2..6)
    // For equal priorities, earlier arrival (smaller request_id) is processed first.
    bool operator()(const Request& a, const Request& b) const
    {
        if (a.priority_level != b.priority_level) {
            return a.priority_level > b.priority_level;
        }
        return a.request_id > b.request_id;
    }
};


struct Mission {
    int request_id;
    int drone_id;
    int destination_sector;
    int start_time;
    int completion_time;

    vector<int> route;
};





#endif