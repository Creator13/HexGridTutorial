using System.Collections.Generic;

public class PriorityQueue<T> where T : class, IPriorityQueueItem {
    private List<T> list = new List<T>();
    private int minimum = int.MaxValue;
    
    public int Count { get; private set; }
    
    public void Enqueue(T item) {
        Count++;
        var priority = item.Priority;

        if (priority < minimum) {
            minimum = priority;
        }
        
        while (priority >= list.Count) {
            list.Add(null);
        }

        item.NextWithSamePriority = list[priority];
        list[priority] = item;
    }

    public T Dequeue() {
        Count--;
        for (; minimum < list.Count; minimum++) {
            var item = list[minimum];
            if (item != null) {
                list[minimum] = (T) item.NextWithSamePriority;
                return item;
            }
        }

        return null;
    }

    public void Change(T item, int oldPriority) {
        var current = list[oldPriority];
        var next = (T) current.NextWithSamePriority;
        if (current == item) {
            list[oldPriority] = next;
        }
        else {
            while (next != item) {
                current = next;
                next = (T) current.NextWithSamePriority;
            }

            current.NextWithSamePriority = item.NextWithSamePriority;
        }
        Enqueue(item);
        Count--;
    }

    public void Clear() {
        list.Clear();
        Count = 0;
        minimum = int.MaxValue;
    }
}

