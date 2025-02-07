[Design Document](https://docs.google.com/document/d/1VFzwZSYIc4VicnQo41ZNIKkjIWGTsbYj6cIFz8xfwDA/edit?usp=sharing)

**Design Document**
**Distributed Prime Number Checking System**
 

This design document is intended for the following professionals: Software Engineers: DevOps Engineers, Security Engineers, and  Software Architects.

‌

---

‌

### **1. Executive Summary**

**Goal**: Provide a distributed service to check if numbers are prime (Prime-Check) at a rate of up to one million requests per second.

- **Scalability**: The system supports dynamic addition/removal of nodes based on load.
- **Reliability & Fault Tolerance**: Because it is distributed, the system is not dependent on a single central server that could become a single point of failure.
- **Performance**: Uses efficient caching (LRU Cache) and replication (DDS) to reduce repeated calculations.
- **Cost Optimization**: External prime computation service is only called when the number’s result is unavailable via local caches or DDS updates.

‌

---

‌

### **2. Introduction**

The system must handle a high volume of requests (up to one million per second) for prime checking. The solution is a distributed architecture that can easily scale out by adding more compute nodes, and scale in when the load decreases.

‌

---

‌

### **3. Overall Architecture**

1. **Load Balancer (LB)**
   - Distributes incoming REST requests among the available compute nodes (Round Robin or other algorithms).
2. **Compute Nodes**
   - Each node has its local in-memory Cache.
   - Communicates via DDS (Data Distribution Service) to share popular prime-check results.
3. **DDS Topic (prime topic)**
   - A shared, decentralized topic where nodes publish and receive updates about frequently requested numbers.
   - Configurable QoS ensures messages persist in memory for a specified duration.
4. **External Prime Service**
   - Invoked only when a requested number’s prime result is unavailable locally  

‌

---

‌

### **4. Architecture Diagram**

‌

![AD\_4nXe2EGjvNEUCz7BKyWze2MCCokcPJoJ5ClmGCKWQNcZdFaY9fWNkIfjZiZiLcXjHeehNFBbew-9EYspZQgCc-RpkKTsu3JHkBS26SSgCCE5s9wQOfXu49K\_mTDk-ioMZwE9\_Lvdn?key=PfOJYZuLKmyPT1wjMdprj\_77](https://lh7-rt.googleusercontent.com/docsz/AD_4nXe2EGjvNEUCz7BKyWze2MCCokcPJoJ5ClmGCKWQNcZdFaY9fWNkIfjZiZiLcXjHeehNFBbew-9EYspZQgCc-RpkKTsu3JHkBS26SSgCCE5s9wQOfXu49K_mTDk-ioMZwE9_Lvdn?key=PfOJYZuLKmyPT1wjMdprj_77)

**Diagram Explanation**:

1. The client sends requests (GET /prime?number=X) to the Load Balancer.

- The Load Balancer routes each request to one of the nodes.
- Each node checks its local cache; call the external prime-check service if not found.
- When a node detects that a certain number is frequently requested ([see policy](https://docs.google.com/document/d/1VFzwZSYIc4VicnQo41ZNIKkjIWGTsbYj6cIFz8xfwDA/edit?tab=t.0#heading=h.9bcdv1jq2b9a "‌")), it publishes the result to the DDS `Topic`. Other nodes update their local caches accordingly.

If a node receives information from the topic regarding the computation of a specific number, it does not re-publish this information back to the topic. This ensures that all nodes are updated only once and prevents unnecessary duplication of updates across the system.

‌

---

‌

### **5. Sequence Diagram**

Below is a sequence diagram to reflect the logic of caching, threshold-based publishing, and how nodes update each other via DDS:

 .

![AD\_4nXdaaWgcTvurlp9L24zTLOOpr\_FJfYLbKbNR6D7I5E-GCIX6MKApqwXhFhxONIeeeyTHkMGD7Z3mYZnHdvT35eGx2zzQpk6WT9LyA3sO2zu7JPPYkMen2UzhPVeBnifc3-4SptMy?key=PfOJYZuLKmyPT1wjMdprj\_77](https://lh7-rt.googleusercontent.com/docsz/AD_4nXdaaWgcTvurlp9L24zTLOOpr_FJfYLbKbNR6D7I5E-GCIX6MKApqwXhFhxONIeeeyTHkMGD7Z3mYZnHdvT35eGx2zzQpk6WT9LyA3sO2zu7JPPYkMen2UzhPVeBnifc3-4SptMy?key=PfOJYZuLKmyPT1wjMdprj_77)

**Key Points**:

- Each node maintains an internal counter of how often a particular number has been requested (locally).
- if the threshold policy is reached, the node publishes that number’s prime-check result to DDS so that **all** other nodes can cache it.
- This strategy prevents excessive broadcast traffic while still sharing hot data across nodes.

‌

---

‌

### **6. Main Components**

#### **6.1 Compute Nodes**

- **Local LRU Cache**: Stores recently computed prime-check results. Evicts the least recently used entries when capacity or time constraints are reached.
- **DDS Client**: Subscribes to the `Topic `for prime-check updates. Publishes results for popular numbers (above a specified threshold).
- **REST Interface**: Responds to GET requests (e.g  /prime?number=X requests).

#### **6.2 DDS Topic**

- **Shared Topic**: All compute nodes are subscribers and publishers.
- **Suggested configuration (QOS)** :
  - Configured to retain messages for a certain duration TTL so nodes can catch up on recently published results.
  - Reliability/Durability ensures that updates are delivered within the configured time window.

#### **6.3 Load Balancer**

- **Even Distribution**: Default round-robin assignment of incoming requests to nodes.
- **Optional Weighted Balancing**: Could base routing decisions on node load metrics in advanced scenarios.

#### **6.4 External Prime Service**

- **Invoked on-demand**: Only if the requested number is not in the local cache or already known via DDS.
- **Response Time**: 1 second compute.
- **Cost**: Billed on every call to the service.

‌

---

‌

### **7. Caching Mechanism**

1. **Local LRU Cache**: Each node stores recent requests/results. Once capacity is reached or the entry expires, the least recently used items are removed first.
2. **Time-to-Live (TTL)**: Limits how long entries remain in the cache.
3. **Selective Publishing to DDS**:
   - Reduces heavy traffic across nodes by only publishing results to DDS after a certain threshold of requests has been reached.
   - Minimizes external service calls in the long run, as frequently requested numbers become known to all nodes.

‌

---

‌

### **8. Scalability**

- **Horizontal Scaling**: Add more compute nodes when the load increases, and remove nodes when the load decreases.
- **Discovery via DDS**: Newly added nodes automatically discover the Topic without manual configuration.
- **Auto-Scaling Mechanism**: This could be triggered by CPU usage, request queue length, or other metrics in cloud environments .

‌

---

‌

### **9. Deployment Considerations**

1. **Availability Zones**: Deploy nodes across multiple zones for higher resilience.
2. **Network Configuration**: DDS typically uses UDP (with multicast or unicast). Ensure the environment supports the required network setup.
3. **Configuration Management**: Maintain parameters (QoS, TTL, LRU size, threshold for publishing) using a centralized configuration service or uniform config files.
4. **Latency & Bandwidth**: Position nodes to minimize network hops to both the external prime service and among themselves. 

‌

### **10. Node Metrics for Externalization**

To maintain the system's performance, reliability, and operational transparency, each compute node will externalize the following key metrics. These metrics are critical for monitoring, optimizing, and troubleshooting the system's distributed architecture:

1. **Cache Hit Rate**
   - The percentage of requests successfully resolved using the local cache, avoiding external computation. A higher rate indicates efficient cache utilization.
2. **Average Response Time**
   - The mean time taken by the node to process and respond to requests, segmented by source:
     - Cache: Requests resolved directly from the local cache.
     - External Service: Requests necessitating computation by the external prime-checking service.
3. **DDS Publish Count**
   - The number of times the node published prime-check results for frequently requested numbers to the DDS topic. This metric reflects the node's contribution to data dissemination across the system.
4. **External Service Calls**
   - The total count of calls made by the node to the external prime-checking service. This metric is closely tied to cost efficiency and response latency.
5. **Resource Utilization**
   - Real-time usage statistics, including:
     - **CPU Usage**: The computational load on the node.
     - **Memory Usage**: The amount of memory consumed, primarily by the cache and DDS client.
     - **Network Utilization**: Bandwidth consumption for communication with DDS and external services.
6. **Error and Failure Count**
   - The number of errors encountered by the node during:
     - Request processing.
     - DDS communication.
     - External service interaction.
   - This metric provides insight into potential issues affecting node reliability.
7. **Node Health Status**
   - A holistic indicator of the node's operational state, with possible statuses such as:
     - Healthy: Fully operational and responsive.
     - Degraded: Experiencing minor performance issues or high load.
     - Unavailable: Unable to process requests or communicate effectively.

‌

‌

### **Threshold Policy for DDS Updates**

- If a number is requested twice within a 60-second window, it is considered "popular." The computation result for that number will be published to the DDS Topic, allowing all other nodes to update their local caches. This policy minimizes redundant computations and optimizes response times for frequently requested numbers.ld policy: 2 requests within the timespan of X sec. default 60 sec
