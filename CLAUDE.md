# Project Instructions

- always use primary constructors when possible.
- use the `field` keyword for databinding.
- SetField is a properly constructed method for handling property changes and notifying observers.
- this is a winui3 project and should be treated as such.
- this is STRICT mvvm pattern. do NOT add methods to the view for any reason other than binding the datacontext.
- do not mess with git for any reason other than me telling you to do so.

# Game Project Folder

- the game project folder is `C:\Users\philb\Documents\Tale Of Betrayal 1`. it is the "database" for the game being built with this game maker: graphics, maps, fighters, items, settings and so on. look here for a game's actual data and assets, not in this repo.
- the active project is set by `GameDirectory` in `C:\Users\philb\Documents\Mercury Game Maker\config.json`.
- **all game art is made with Recraft (recraft.ai).** read `C:\Users\philb\Documents\Tale Of Betrayal 1\ArtGuide.md` before any art work: it has the Recraft project link, the art direction, the sprite format, the workflow and the approved character designs.
- the story bible is `C:\Users\philb\Documents\Tale Of Betrayal 1\Story.md`.

# Claude Instructions

- after any large changes run a dotnet build to make sure everything is working correctly.
- i want a list of all the files changed at the end of your summary. It should look like this:
  ```
   - EnemyPage.xaml +5 -82
  ```

  the +5 is the number of lines added to the file, and the -82 is the number of lines removed from the file.
- only list files changed in the current turn. If nothing changed this turn, leave the list out.
