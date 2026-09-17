/// <summary>
/// Interface cho mọi vật thể có thể tương tác.
/// Gắn component kế thừa interface này vào Door, Button, Item, v.v.
/// </summary>
public interface IInteractable
{
    /// <summary>Tên hiển thị trên UI prompt (vd: "Cửa", "Nút bấm")</summary>
    string InteractLabel { get; }

    /// <summary>Gọi khi Player nhấn phím tương tác (E)</summary>
    void Interact();

    /// <summary>Có thể tương tác lúc này không? (vd: false nếu cửa đang mở animation)</summary>
    bool CanInteract { get; }
}

