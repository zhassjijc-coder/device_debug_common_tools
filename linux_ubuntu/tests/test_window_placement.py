import os
import unittest
from unittest.mock import patch

os.environ.setdefault("QT_QPA_PLATFORM", "offscreen")
from PySide6.QtCore import QRect
from PySide6.QtWidgets import QApplication
from linklab.ui.main_window import MainWindow
from linklab.ui.window_placement import startup_bounds

APP = QApplication.instance() or QApplication([])


class WindowPlacementTests(unittest.TestCase):
    def test_bounds_fit_small_scaled_portrait_and_negative_origin_desktops(self):
        for area in (QRect(0, 0, 1366, 728), QRect(0, 0, 1280, 680),
                     QRect(0, 0, 853, 440), QRect(-1707, 40, 1707, 1027),
                     QRect(2560, -900, 1080, 1880), QRect(0, 0, 3840, 2120)):
            with self.subTest(area=area):
                bounds = startup_bounds(area, 16, 39)
                self.assertTrue(area.contains(bounds))
                self.assertLessEqual(abs(bounds.center().x() - area.center().x()), 1)
                self.assertLessEqual(abs(bounds.center().y() - area.center().y()), 1)

    def test_first_show_fits_frame_but_tray_restore_keeps_user_geometry(self):
        window = MainWindow(test_mode=True)
        try:
            window.show()
            APP.processEvents()

            area = window.screen().availableGeometry()
            frame = window.frameGeometry()
            self.assertTrue(area.contains(frame), (area, frame, window.minimumSize()))
            self.assertLessEqual(abs(area.center().x() - frame.center().x()), 2)
            self.assertLessEqual(abs(area.center().y() - frame.center().y()), 2)
            self.assertLessEqual(window.minimumWidth(), window.width())
            self.assertLessEqual(window.minimumHeight(), window.height())
            window.move(frame.x() + 7, frame.y() + 9)
            expected = window.geometry()
            window.hide()
            with patch("linklab.ui.main_window.fit_startup_window") as fit:
                window.show()
                APP.processEvents()
                fit.assert_not_called()
            self.assertEqual(window.geometry(), expected)
        finally:
            window.close()
            APP.processEvents()


    def test_small_work_area_overrides_old_minimum_size(self):
        from PySide6.QtGui import QScreen
        for area in (QRect(0, 0, 853, 440), QRect(0, 0, 1366, 728)):
            with self.subTest(area=area), patch.object(QScreen, "availableGeometry", return_value=area):
                window = MainWindow(test_mode=True)
                try:
                    window.show()
                    APP.processEvents()
                    self.assertTrue(area.contains(window.frameGeometry()), (area, window.frameGeometry()))
                finally:
                    window.close()
                    APP.processEvents()


if __name__ == "__main__":
    unittest.main()
