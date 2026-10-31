"""Initial main-window placement in Qt logical coordinates, including the OS frame."""
from PySide6.QtCore import QRect
from PySide6.QtGui import QCursor
from PySide6.QtWidgets import QApplication


def startup_bounds(available, frame_width=0, frame_height=0):
    width = min(1400 + frame_width, int(available.width() * .92))
    height = min(900 + frame_height, int(available.height() * .90))
    return QRect(available.x() + (available.width() - width) // 2,
                 available.y() + (available.height() - height) // 2, width, height)


def fit_startup_window(window, screen=None):
    screen = screen or QApplication.screenAt(QCursor.pos()) or window.screen() or QApplication.primaryScreen()
    if screen is None:
        return
    window.setScreen(screen)
    # Qt already reports device-independent coordinates; do not multiply by DPR.
    frame = window.frameGeometry()
    extra_width = max(0, frame.width() - window.width())
    extra_height = max(0, frame.height() - window.height())
    bounds = startup_bounds(screen.availableGeometry(), extra_width, extra_height)
    width = max(1, bounds.width() - extra_width)
    height = max(1, bounds.height() - extra_height)
    window.setMinimumSize(min(1000, width), min(680, height))
    window.resize(width, height)
    # QWidget.move positions the outer frame of a top-level window.
    window.move(bounds.topLeft())
