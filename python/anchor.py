# Widget prototyping
from typing import Union

class Widget:
    def __init__(self) -> None:
        self.parent: Union[Widget,None] = None

    def compute_desired_size(self) -> tuple[int,int]:
        pass

class Slot:
    def __init__(self,widget: Widget) -> None:
        self.widget = widget

class Anchor:
    def __init__(self,min = 0.0,max = 0.0) -> None:
        self.min = min
        self.max = max

class Rect:
    def __init__(self) -> None:
        self.x = 0
        self.y = 0
        self.width = 0
        self.height = 0

class CanvasSlot(Slot):
    def __init__(self, widget: Widget) -> None:
        super().__init__(widget)
        self.anchor_x = Anchor()
        self.anchor_y = Anchor()
        self.rect = Rect()

class Canvas(Widget):
    def __init__(self) -> None:
        self.width = 1920
        self.height = 1080
        self.children: list[CanvasSlot] = []

    def add_child(self,widget: Widget) -> CanvasSlot:
        slot = CanvasSlot(widget)

        self.children.append(slot)

        return slot
    
    def compute_rect_in_canvas(self,slot: CanvasSlot) -> tuple[int,int,int,int]:
        x1,x2,y1,y2 = slot.rect.x,slot.rect.x + slot.rect.width,slot.rect.y,slot.rect.y + slot.rect.height
        
        x1 = (self.width * slot.anchor_x.min) + x1
        x2 = (self.width * slot.anchor_x.max) + x2
        y1 = (self.height * slot.anchor_y.min) + y1
        y2 = (self.height * slot.anchor_y.max) + y2

        return x1,y1,x2 - x1,y2 - y1
        

root = Canvas()

slot = root.add_child(Widget())

slot.rect.width = 256
slot.rect.height = 256
slot.rect.x = 0 #10
slot.rect.y = 0 #10


final_position = root.compute_rect_in_canvas(slot)

slot.anchor_x = Anchor(0.5,0.5)

print(final_position)

root.width = root.width / 2

final_position = root.compute_rect_in_canvas(slot)

print(final_position)
