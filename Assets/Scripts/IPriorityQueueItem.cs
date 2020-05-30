public interface IPriorityQueueItem {
    int Priority { get; }
    
    IPriorityQueueItem NextWithSamePriority { get; set; }
}

