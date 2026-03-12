# Real-time multi-agent traffic simulation visualized in Unity

Link to Blog: https://ok26.github.io/traffic-simulation/

## Currently implemented nodes
#### 0: Endpoint
A node without any real logic, which can be used as a start/finish point.

#### 1: Trafic light intersection
Simulates an intersection with traffic lights with green, yellow and red lights.

#### 2: Stop sign intersection
Simulates an intersection with stop signs in each direction. 

## Directions and connections of nodes
Node types 0-2 use the following format: 

### Intersection Direction
```
      0
      |
3 ----O---- 2
      |
      1
```
### Lane Connections
```
      0  1
      |  |
3 ---- /\ ---- 4
2 ---- \/ ---- 5        
      |  |
      7  6
```


## How to create networks
Create your desired road network in "Assets/Resources/network.txt"

The format of the file is the following: 

```

**n**  *Number of nodes*

*n number of individual nodes follow:* 

**t**  *Type of node*

**x z**  *Coordinates in floats*

...

**r** *Number of roads*

*r number of individual roads follow:*

**n0 n1**  *The index of connected nodes(indices are in order of the previous list of nodes)*

**d0 d1**  *The direction in which to enter a node (See directions of nodes above)*

**ctrlp1 ctrlp2** *Bezier control points of the road*

**rl ll**  *Number of lanes (right & left)*

*rl number of right lanes:*

**c0 c1**  *The connections in the nodes for this specific lane (See connections of nodes above)*

...

*ll number of left lanes:*

**c0 c1**

...

```
