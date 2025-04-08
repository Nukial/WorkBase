using System;
using System.Collections.Generic;
using UnityEngine; // Vẫn cần cho Debug.LogError nếu bạn dùng Unity

// Lớp cơ sở cho tất cả các đối số sự kiện tùy chỉnh
public class EventArgsBase : EventArgs
{
    // Có thể thêm các thuộc tính hoặc phương thức chung cho tất cả sự kiện ở đây nếu cần
}

// Lớp quản lý việc đăng ký, hủy đăng ký và kích hoạt sự kiện
public class EventDispatcher
{
    // Sử dụng object làm lock để đảm bảo thread-safety
    private readonly object _lock = new object();

    // Dictionary để lưu trữ các sự kiện.
    // Key: Type của EventArgs
    // Value: Danh sách các delegate (các hàm xử lý)
    private Dictionary<Type, List<Delegate>> eventListeners = new Dictionary<Type, List<Delegate>>();

    // Đăng ký sự kiện
    public void Subscribe<T>(Action<T> listener) where T : EventArgsBase
    {
        Type eventType = typeof(T);
        // Sử dụng lock để đảm bảo thread safety khi sửa đổi dictionary/list
        lock (_lock)
        {
            if (!eventListeners.TryGetValue(eventType, out var listeners))
            {
                listeners = new List<Delegate>();
                eventListeners.Add(eventType, listeners);
            }

            // Ngăn đăng ký trùng lặp
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
            // Optional: Ghi log nếu listener đã tồn tại
            // else
            // {
            //     Debug.LogWarning($"Listener already subscribed for event type {eventType}.");
            // }
        }
    }

    // Hủy đăng ký sự kiện
    public void Unsubscribe<T>(Action<T> listener) where T : EventArgsBase
    {
        Type eventType = typeof(T);
        // Sử dụng lock để đảm bảo thread safety
        lock (_lock)
        {
            if (eventListeners.TryGetValue(eventType, out var listeners))
            {
                listeners.Remove(listener);

                // Dọn dẹp entry nếu list rỗng
                if (listeners.Count == 0)
                {
                    eventListeners.Remove(eventType);
                }
            }
        }
    }

    // Kích hoạt sự kiện
    public void Raise<T>(T eventArgs) where T : EventArgsBase
    {
        Type eventType = typeof(T);
        List<Delegate> listenersSnapshot = null; // Tạo bản sao để duyệt

        // Chỉ lock khi lấy danh sách listener, giảm thời gian lock
        lock (_lock)
        {
            if (eventListeners.TryGetValue(eventType, out var listeners))
            {
                // Tạo bản sao (snapshot) để tránh lỗi khi collection bị thay đổi
                listenersSnapshot = new List<Delegate>(listeners);
            }
        }

        // Nếu có listener thì mới duyệt và gọi (ngoài lock)
        if (listenersSnapshot != null)
        {
            foreach (Delegate listenerDelegate in listenersSnapshot)
            {
                // Ép kiểu delegate sang Action<T>
                Action<T> action = listenerDelegate as Action<T>;

                if (action != null)
                {
                    // Xử lý lỗi từng listener
                    try
                    {
                        action(eventArgs); // Gọi listener
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi thay vì dừng toàn bộ quá trình
                        // Sử dụng Debug.LogError nếu trong môi trường Unity
                        Console.WriteLine($"Error executing event listener for {eventType}: {ex}");
                        // Hoặc Debug.LogError($"Error executing event listener for {eventType}: {ex}");
                    }
                }
                else
                {
                    // Log lỗi nếu kiểu delegate không đúng (hiếm khi xảy ra với cấu trúc này)
                     Console.WriteLine($"Invalid listener type found in event dispatcher for {eventType}. Expected Action<{typeof(T).Name}> but got {listenerDelegate.GetType().Name}");
                     // Hoặc Debug.LogError(...)
                }
            }
        }
    }
}