# Mission logic is written as trigger lists

Authors program deeper Mission logic as Warcraft III-style trigger lists (when / if / do, chosen from dropdowns) plus Variables. A node graph was rejected because it has the same power for far more UI work and gets hard to read at scale. Text scripting inside Missions was rejected because shared files would carry code that must be sandboxed, and every function exposed to scripts becomes a permanent promise. Missions therefore stay plain data.
