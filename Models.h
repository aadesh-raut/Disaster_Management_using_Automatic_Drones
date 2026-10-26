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
    bool operator()(const Request& a, const Request& b)
    {
        return a.priority_level < b.priority_level;
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