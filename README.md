
# Conway

## Dev Setup
Create a database named "Conway" (or whatever you want) in your local SQL Server instance. Add the connection string to your user secrets for the project.
The app will take care of creating the needed structures in the db when it runs.

Compile and run, Swagger should pop up in a browser tab and you can explore the API from there.

### Conway Game
`conway.linq` is available in the root directory and can be opened with LINQPad:
https://www.linqpad.net/Download.aspx

You can specify dimensions and a random Conway game will be started, animated, and run to conclusion (either an end state or once a loop is detected).

*Note*: don't copy and paste the contents of the .linq file - open LINQPad and use File > Open.

## Notes
Look for `TODO` notes in the project - most will be in the `Program.cs`, but some tests are stubbed out as well. These are noting areas for future development.
There are additional notes outlining architecture decisions and my reasoning.