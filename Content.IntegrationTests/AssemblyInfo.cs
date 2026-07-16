[assembly: Parallelizable(ParallelScope.Children)]

// Serv5 removed expression tree spam so we can raise parallelism, however entity serialization and prototypes in general still
// limit things a bit.
// Excessive parallelism levels also obliterates system memory so be mindful of that. Smile.
[assembly: LevelOfParallelism(4)]
