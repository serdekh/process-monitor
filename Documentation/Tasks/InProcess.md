### InProcess Tasks

`Fix`
- Refactor the 'Transport' layer
  - The server currently uses a prototype version of the 'Transport' layer. It has to be refactored to support a strongly typed Result value for the returns instead of the nullable exceptions. And it also needs to he properly designed to be testable with interfaces.