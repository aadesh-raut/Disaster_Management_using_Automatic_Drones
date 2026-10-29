
cmake_minimum_required(VERSION 3.20)

project(DisasterManagement LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

find_package(Threads REQUIRED)
find_package(Crow CONFIG REQUIRED)

# Terminal-based simulation
add_executable(drone_rescue
    main.cpp
    RescueSystem.cpp
)

# HTTP API server for Unity
add_executable(api_server
    api_server.cpp
    RescueSystem.cpp
)

target_link_libraries(api_server PRIVATE
    Crow::Crow
    Threads::Threads
)
