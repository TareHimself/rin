#include "rin/widgets/slots/WSPanel.hpp"

#include "rin/widgets/containers/WCPanel.hpp"

WSPanel::WSPanel(WCPanel* panel, const Shared<Widget>& widget) : ContainerWidgetSlot(panel, widget)
{
}
