# FFVIII-TripleTriad-Unity3D

## Overview

This project is a simple implementation of Triple Triad, inspired by Final Fantasy VIII, developed using Unity3D. It allows for both 2-player games and games against Reinforcement Learning agents. The AI agents are based on MaskablePPO from stable-baselines3 and AlphaZero, both of which are developed by me. The project includes card images, board, and sounds, with assets sourced from [crystal-bit/triple-triad-godot](https://github.com/crystal-bit/triple-triad-godot/).

## Player vs Player Video
[![FFVIII-TripleTriad-Unity3D](https://img.youtube.com/vi/o7f87ufDypM/0.jpg)](https://youtu.be/o7f87ufDypM)

## Player vs AI Video


## Features

- Play Triple Triad against another player or against AI agents.
- The game is currently programmed to automatically balance cards.
- Only rule available is Open, for now.
- AI agents include:
  - MaskablePPO (based on stable-baselines3)
  - AlphaZero (still haven't got to a good playing model)
- The Unity3D game communicates with local servers from each project for AI inference.

## Usage

### Player vs Player
1. Start the Unity game.
2. Choose ***vs Player***.
3. Enjoy the game!

### Player vs AI
1. Ensure at least one AI server is running locally.
2. Select the `AIPlayer` script in the `AI` GameObject in the inspector and choose if playing against AlphaZero or not (default is MaskablePPO)
3. Start the Unity game.
4. Choose ***vs AI***.
5. Enjoy the game!
   
## Game Controls

- **Mouse**: Click to select and place cards.

## Acknowledgements

Card images, board, and sounds are sourced from [crystal-bit/triple-triad-godot](https://github.com/crystal-bit/triple-triad-godot). Special thanks to the contributors of this repository.

## Contributing

If you want to contribute to this project, please fork the repository and submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Project Status

This project is a work in progress. While the MaskablePPO agent (based on stable-baselines3) is functioning well, the AlphaZero agent is still under development. Currently, the AlphaZero model hasn't reached a level of play that is competitive.

Later on, SAME, PLUS, CLOSE and SUDDEN DEATH rules will be added.
