import os
import sys
import time
from typing import Callable

class FileWatcher:
    def __init__(self,files: list[str],callback: Callable[[str],None],poll_interval = 0.5) -> None:
        self.files = files
        self.timestamps: dict[str,float] = {}
        self.poll_interval = poll_interval
        self.callback = callback
    
    def watch(self):

        while True:
            for file in self.files:
                last_modified = os.path.getmtime(file)
                if file in self.timestamps.keys() and self.timestamps[file] != last_modified:
                    self.callback(file)
                
                self.timestamps[file] = last_modified
                
            time.sleep(self.poll_interval)