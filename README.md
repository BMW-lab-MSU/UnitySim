A notion document that shows you how to set it up can be viewed here https://www.notion.so/Unity-Simulation-Tutorial-bb29a65559d5470da24721c54cf834c1?pvs=4

### schargois/reu Branch Updates

This branch has been modified for the 'Effects of Graphical Fidelity on
Simulation-To-Reality Object Detection for Underwater Environments' REU project
by Sterling Chargois (schargois). Some distinct differences are the addition of
the auto_camera.cs file to automatically move the file. Instructions are documented
in the code. It can be activated by pressing 'G' on the keyboard while running.

Also, due to object and label changes. The pond scene may not label correctly in
the current state of the 'items.asset' file. There exist 4 mesh quality levels of
each object for the lake and pool scenes as well.

Outside of the Unity simulation, python scripts have been added to help convert
datasets or count what labels they have. Instructions are documented in the files.

There are two files 'lake_poses.json' and 'pool_poses.json' located outside of
'ROBOSUB-UNITY'. These are usedby auto_camera.py . They contain the repeated
movements used to collect data in the REU project.
