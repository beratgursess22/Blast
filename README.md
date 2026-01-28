Project Overview#
--------------------------------
This is a tile-based collapse/blast puzzle game where the player removes groups of adjacent blocks with the same color. When blocks are removed, remaining blocks fall down due to gravity and new blocks spawn from the top. The system is designed to be fully configurable via the Unity Inspector, allowing easy testing of different board sizes, color counts, and gameplay scenarios.

Core Features#
--------------------------------
A.Configurable Board Size (M × N)
Board dimensions can be changed dynamically via the Inspector (rows and columns).

B.Configurable Color Count (K)
Number of colors can be adjusted between 1 and 6.

C.Group-Based Blast Mechanic
1-Minimum group size to blast: 2
2-roups are detected using a flood-fill (BFS) algorithm.

D.Gravity-Based Refill System
1-Blocks above fall down smoothly with animation.
2-New blocks spawn from above the visible area.

E.Group Size Based Icon Upgrade (A / B / C)
Larger groups are visually emphasized using upgraded icons:
1-Default icon for small groups
2-A,B,C tier icons based on configurable group size thresholds

F.Deadlock Detection
The system checks whether there are any possible moves left on the board.

G.Guaranteed Shuffle (Non-Blind Shuffle)
When a deadlock occurs, the board is reshuffled in a way that guarantees at least one valid move, instead of repeatedly shuffling blindly.

L.Performance-Oriented Design
1-Object Pooling is used instead of frequent Instantiate/Destroy calls.
2-Animations and visual updates are synchronized to avoid unnecessary state changes.

Technical Implementation Notes#
--------------------------------
1.Board Representation
The board is stored as a 2D grid structure, where each cell references a block instance.

2.Group Detection
A flood-fill algorithm (BFS) is used to detect connected blocks of the same color.

3.Animation Synchronization
Active movement counters ensure that visual updates (icon changes, shuffles) occur only after all block movements are completed, preventing visual artifacts.

4.Inspector-Driven Design
Most gameplay parameters (board size, colors, thresholds, speeds) are exposed to the Inspector for fast iteration and testing.


<img width="349" height="581" alt="Screenshot 2026-01-28 at 14 30 07" src="https://github.com/user-attachments/assets/7c9110b8-47a6-482f-9ac3-7377218d5869" />


