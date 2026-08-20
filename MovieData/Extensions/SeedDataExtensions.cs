using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieCore.Models;

namespace MovieData.Extensions;

public static class SeedDataExtensions
{
    public static void SeedData(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MovieContext>();
        context.Database.Migrate();
        if (context.Movies.Any()) return; // already seeded, idempotency guard

// --- Genres (ids 1..4 on a fresh DB) ---
        var drama = new Genre { Name = "Drama" };
        var comedy = new Genre { Name = "Comedy" };
        var documentary = new Genre { Name = Genres.Documentary }; // single source of truth
        var sciFi = new Genre { Name = "Sci-Fi" };
        context.Genres.AddRange(drama, comedy, documentary, sciFi);

// --- Actors (ids 1..14) ---
        var hanks = new Actor { Name = "Tom Hanks", BirthYear = 1956 };
        var robbins = new Actor { Name = "Tim Robbins", BirthYear = 1958 };
        var freeman = new Actor { Name = "Morgan Freeman", BirthYear = 1937 };
        var johansson = new Actor { Name = "Scarlett Johansson", BirthYear = 1984 };
        var murray = new Actor { Name = "Bill Murray", BirthYear = 1950 };
// extra cast (ids 6..14) so the Documentary can hold 10 actors and the cap is testable
        var attenborough = new Actor { Name = "David Attenborough", BirthYear = 1926 };
        var herzog = new Actor { Name = "Werner Herzog", BirthYear = 1942 };
        var weaver = new Actor { Name = "Sigourney Weaver", BirthYear = 1949 };
        var jones = new Actor { Name = "James Earl Jones", BirthYear = 1931 };
        var irons = new Actor { Name = "Jeremy Irons", BirthYear = 1948 };
        var mirren = new Actor { Name = "Helen Mirren", BirthYear = 1945 };
        var neeson = new Actor { Name = "Liam Neeson", BirthYear = 1952 };
        var blanchett = new Actor { Name = "Cate Blanchett", BirthYear = 1969 };
        var elba = new Actor { Name = "Idris Elba", BirthYear = 1972 };
// supporting players for the original six — casts read in billing order,
// so the last name is the one the quiz's free clue leaks
        var wright = new Actor { Name = "Robin Wright", BirthYear = 1966 };
        var sinise = new Actor { Name = "Gary Sinise", BirthYear = 1955 };
        var gunton = new Actor { Name = "Bob Gunton", BirthYear = 1945 };
        var sadler = new Actor { Name = "William Sadler", BirthYear = 1950 };
        var ribisi = new Actor { Name = "Giovanni Ribisi", BirthYear = 1974 };
        var faris = new Actor { Name = "Anna Faris", BirthYear = 1976 };
        var macdowell = new Actor { Name = "Andie MacDowell", BirthYear = 1958 };
        var tobolowsky = new Actor { Name = "Stephen Tobolowsky", BirthYear = 1951 };
        var adams = new Actor { Name = "Amy Adams", BirthYear = 1974 };
        var mara = new Actor { Name = "Rooney Mara", BirthYear = 1985 };
        context.Actors.AddRange(hanks, robbins, freeman, johansson, murray,
            attenborough, herzog, weaver, jones, irons, mirren, neeson, blanchett, elba,
            wright, sinise, gunton, sadler, ribisi, faris, macdowell, tobolowsky, adams, mara);

// --- Movies (ids 1..6) — varied genres, shared actors, uneven review counts ---
        var movies = new List<Movie>
        {
            new()
            {
                Title = "Forrest Gump", Year = 1994, Duration = 142,
                Genres = { drama },
                Cast =
                {
                    new() { Actor = hanks, Role = "Forrest Gump" },
                    new() { Actor = wright, Role = "Jenny Curran" },
                    new() { Actor = sinise, Role = "Lieutenant Dan Taylor" }
                },
                Details = new MovieDetails
                    { Synopsis = "Life is like a box of chocolates", Language = "English", Director = "Robert Zemeckis", Budget = 55_000_000m },
                Reviews =
                {
                    new Review { ReviewerName = "Alice", Comment = "Classic!", Rating = 5 },
                    new Review { ReviewerName = "Bob", Comment = "Touching.", Rating = 4 }
                }
            },
            new()
            {
                Title = "The Shawshank Redemption", Year = 1994, Duration = 142,
                Genres = { drama },
                Cast =
                {
                    new() { Actor = robbins, Role = "Andy Dufresne" },
                    new() { Actor = freeman, Role = "Ellis 'Red' Redding" },
                    new() { Actor = gunton, Role = "Warden Norton" },
                    new() { Actor = sadler, Role = "Heywood" }
                },
                Details = new MovieDetails
                    { Synopsis = "The story of a banker who spends nineteen years in Shawshank prison for a murder he did not commit, and of the redemption he finds in friendship.", Language = "English", Director = "Frank Darabont", Budget = 25_000_000m },
                Reviews =
                {
                    new Review { ReviewerName = "Cara",   Comment = "Masterpiece.",   Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-8) },
                    new Review { ReviewerName = "Dan",    Comment = "Hopeful.",       Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-7) },
                    new Review { ReviewerName = "Eve",    Comment = "Slow start.",    Rating = 3, CreatedAt = DateTime.UtcNow.AddDays(-6) },
                    new Review { ReviewerName = "Greta",  Comment = "Unforgettable.", Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new Review { ReviewerName = "Hans",   Comment = "A classic.",     Rating = 4, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                    new Review { ReviewerName = "Ingrid", Comment = "Powerful.",      Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new Review { ReviewerName = "Jonas",  Comment = "Moving.",        Rating = 5, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new Review { ReviewerName = "Karin",  Comment = "Brilliant.",     Rating = 4, CreatedAt = DateTime.UtcNow.AddDays(-1) }
                }
            },
            new()
            {
                Title = "Lost in Translation", Year = 2003, Duration = 102,
                Genres = { drama, comedy },
                Cast =
                {
                    new() { Actor = murray, Role = "Bob Harris", Billing = 0 },
                    new() { Actor = johansson, Role = "Charlotte", Billing = 1 },
                    new() { Actor = ribisi, Role = "John", Billing = 2 },
                    new() { Actor = faris, Role = "Kelly", Billing = 3 }
                },
                Details = new MovieDetails
                    { Synopsis = "An aging movie star and a young wife drift through Tokyo nights, lost between time zones and marriages, sharing something neither can quite translate.", Language = "English", Director = "Sofia Coppola", Budget = 4_000_000m },
                Reviews = { new Review { ReviewerName = "Finn", Comment = "Quietly great.", Rating = 4 } }
            },
            new()
            {
                Title = "Groundhog Day", Year = 1993, Duration = 101,
                Genres = { comedy },
                Cast =
                {
                    new() { Actor = murray, Role = "Phil Connors" },
                    new() { Actor = macdowell, Role = "Rita Hanson" },
                    new() { Actor = tobolowsky, Role = "Ned Ryerson" }
                },
                Details = new MovieDetails
                    { Synopsis = "A cynical TV weatherman covering the annual Groundhog Day festivities in Punxsutawney wakes up to the same day, over and over, until he gets it right.", Language = "English", Director = "Harold Ramis", Budget = 14_600_000m }
                // no reviews — exercises the zero-review case
            },
            new()
            {
                Title = "March of the Penguins", Year = 2005, Duration = 80,
                Genres = { documentary },
                // 10 actors — at the Documentary cap, so an 11th POST → 400
                Actors = { freeman, attenborough, herzog, weaver, jones, irons, mirren, neeson, blanchett, elba },
                Details = new MovieDetails
                    { Synopsis = "Every winter, thousands of emperor penguins march across the Antarctic ice to their breeding grounds, each pair guarding a single egg through the polar night.", Language = "French", Director = "Luc Jacquet", Budget = 8_000_000m },
                Reviews = { new Review { ReviewerName = "Gil", Comment = "Beautiful.", Rating = 4 } }
            },
            new()
            {
                Title = "Her", Year = 2013, Duration = 126,
                Genres = { drama, sciFi },
                Cast =
                {
                    new() { Actor = johansson, Role = "Samantha (voice)" },
                    new() { Actor = adams, Role = "Amy" },
                    new() { Actor = mara, Role = "Catherine" }
                },
                Details = new MovieDetails
                    { Synopsis = "In a near-future Los Angeles, a lonely writer falls for Samantha, an operating system with a voice all her own.", Language = "English", Director = "Spike Jonze", Budget = 23_000_000m },
                // 9 reviews — recent movie (2013), so the 10-cap applies; one more POST hits 10, the next 400s
                Reviews =
                {
                    new Review { ReviewerName = "Hana", Comment = "Melancholic.", Rating = 5 },
                    new Review { ReviewerName = "Ivan", Comment = "Thought-provoking.", Rating = 4 },
                    new Review { ReviewerName = "Judy", Comment = "Beautifully shot.", Rating = 5 },
                    new Review { ReviewerName = "Kyle", Comment = "Unsettling and tender.", Rating = 4 },
                    new Review { ReviewerName = "Lena", Comment = "Loved the score.", Rating = 5 },
                    new Review { ReviewerName = "Milo", Comment = "A touch slow.", Rating = 3 },
                    new Review { ReviewerName = "Nora", Comment = "The future feels real.", Rating = 4 },
                    new Review { ReviewerName = "Omar", Comment = "Heartbreaking.", Rating = 5 },
                    new Review { ReviewerName = "Pia", Comment = "Bittersweet.", Rating = 4 }
                }
            }
        };

        context.Movies.AddRange(movies);

// --- A8: the 90s–00s wall — data-driven; one tuple per film, the loop below
//     builds the graph. Cast tuples read in billing order: the LAST name is
//     the supporting player that the quiz's free clue leaks.
        var moreGenres = new[]
            {
                "Crime", "Thriller", "Action", "Animation", "Romance",
                "War", "Fantasy", "Adventure", "Mystery", "Horror", "Western"
            }
            .ToDictionary(name => name, name => new Genre { Name = name });
        context.Genres.AddRange(moreGenres.Values);

        var genreByName = new Dictionary<string, Genre>
        {
            ["Drama"] = drama, ["Comedy"] = comedy,
            [Genres.Documentary] = documentary, ["Sci-Fi"] = sciFi
        };
        foreach (var (name, genre) in moreGenres) genreByName[name] = genre;

        var actorByName = new[]
            {
                hanks, robbins, freeman, johansson, murray, attenborough, herzog,
                weaver, jones, irons, mirren, neeson, blanchett, elba, wright, sinise,
                gunton, sadler, ribisi, faris, macdowell, tobolowsky, adams, mara
            }
            .ToDictionary(actor => actor.Name);
        Actor ActorOf(string name, int born)
        {
            if (!actorByName.TryGetValue(name, out var actor))
            {
                actor = new Actor { Name = name, BirthYear = born };
                actorByName[name] = actor;
                context.Actors.Add(actor);
            }
            return actor;
        }

        var wall = new (string Title, int Year, int Min, string[] Genres, string Language,
            string Director, decimal BudgetM, (string Name, int Born, string Role)[] Cast,
            string Synopsis)[]
        {
            ("Goodfellas", 1990, 145, new[] { "Crime", "Drama" }, "English", "Martin Scorsese", 25m,
                new[] { ("Ray Liotta", 1954, "Henry Hill"), ("Robert De Niro", 1943, "Jimmy Conway"), ("Paul Sorvino", 1939, "Paul Cicero") },
                "Henry Hill grows up wanting to be a gangster and learns, one body at a time, how goodfellas bury their own."),
            ("Dances with Wolves", 1990, 181, new[] { "Western", "Drama" }, "English", "Kevin Costner", 22m,
                new[] { ("Kevin Costner", 1955, "John Dunbar"), ("Mary McDonnell", 1952, "Stands With A Fist"), ("Graham Greene", 1952, "Kicking Bird") },
                "A Civil War officer posted to the empty frontier is adopted by the Lakota and dances away from his old name."),
            ("Edward Scissorhands", 1990, 105, new[] { "Fantasy", "Romance" }, "English", "Tim Burton", 20m,
                new[] { ("Johnny Depp", 1963, "Edward"), ("Winona Ryder", 1971, "Kim"), ("Alan Arkin", 1934, "Bill") },
                "An unfinished boy with scissorhands is taken in by suburbia, which loves his hedges and fears his hands."),
            ("Terminator 2: Judgment Day", 1991, 137, new[] { "Action", "Sci-Fi" }, "English", "James Cameron", 102m,
                new[] { ("Arnold Schwarzenegger", 1947, "The Terminator"), ("Linda Hamilton", 1956, "Sarah Connor"), ("Edward Furlong", 1977, "John Connor") },
                "A reprogrammed terminator is sent back to guard the boy who must live to face judgment day."),
            ("The Silence of the Lambs", 1991, 118, new[] { "Thriller", "Crime" }, "English", "Jonathan Demme", 19m,
                new[] { ("Jodie Foster", 1962, "Clarice Starling"), ("Anthony Hopkins", 1937, "Hannibal Lecter"), ("Ted Levine", 1957, "Buffalo Bill") },
                "A trainee agent trades pieces of herself for a caged doctor's help, until the silence of the lambs ends."),
            ("Unforgiven", 1992, 130, new[] { "Western", "Drama" }, "English", "Clint Eastwood", 14m,
                new[] { ("Clint Eastwood", 1930, "William Munny"), ("Gene Hackman", 1930, "Little Bill Daggett"), ("Jaimz Woolvett", 1967, "The Schofield Kid") },
                "A retired killer takes one last bounty and finds that out here nobody rides away unforgiven."),
            ("Reservoir Dogs", 1992, 99, new[] { "Crime", "Thriller" }, "English", "Quentin Tarantino", 1.2m,
                new[] { ("Harvey Keitel", 1939, "Mr. White"), ("Tim Roth", 1961, "Mr. Orange"), ("Chris Penn", 1965, "Nice Guy Eddie") },
                "Six strangers in matching suits crawl away from a heist gone wrong, snapping at each other like reservoir dogs."),
            ("Aladdin", 1992, 90, new[] { "Animation", "Comedy" }, "English", "Ron Clements", 28m,
                new[] { ("Scott Weinger", 1975, "Aladdin (voice)"), ("Robin Williams", 1951, "Genie (voice)"), ("Gilbert Gottfried", 1955, "Iago (voice)") },
                "A street kid rubs the wrong lamp, wins a loud blue friend, and Aladdin talks his way into a palace."),
            ("Schindler's List", 1993, 195, new[] { "Drama", "War" }, "English", "Steven Spielberg", 22m,
                new[] { ("Liam Neeson", 1952, "Oskar Schindler"), ("Ben Kingsley", 1943, "Itzhak Stern"), ("Embeth Davidtz", 1965, "Helen Hirsch") },
                "An industrialist spends his fortune buying names, and Schindler's list becomes a lifeboat made of paper."),
            ("Jurassic Park", 1993, 127, new[] { "Adventure", "Sci-Fi" }, "English", "Steven Spielberg", 63m,
                new[] { ("Sam Neill", 1947, "Alan Grant"), ("Laura Dern", 1967, "Ellie Sattler"), ("Wayne Knight", 1955, "Dennis Nedry") },
                "A billionaire opens a park of resurrected dinosaurs, and the Jurassic guests decide the fences are a suggestion."),
            ("The Fugitive", 1993, 130, new[] { "Thriller", "Action" }, "English", "Andrew Davis", 44m,
                new[] { ("Harrison Ford", 1942, "Richard Kimble"), ("Tommy Lee Jones", 1946, "Samuel Gerard"), ("Sela Ward", 1956, "Helen Kimble") },
                "A surgeon convicted of his wife's murder runs, and the fugitive hunts the one-armed man while a marshal hunts him."),
            ("Pulp Fiction", 1994, 154, new[] { "Crime", "Drama" }, "English", "Quentin Tarantino", 8m,
                new[] { ("John Travolta", 1954, "Vincent Vega"), ("Samuel L. Jackson", 1948, "Jules Winnfield"), ("Harvey Keitel", 1939, "Winston Wolf") },
                "Hit men, a boxer and a kingpin's wife spin through pulp tales told gloriously out of order."),
            ("Léon", 1994, 110, new[] { "Crime", "Thriller" }, "English", "Luc Besson", 16m,
                new[] { ("Jean Reno", 1948, "Léon"), ("Natalie Portman", 1981, "Mathilda"), ("Danny Aiello", 1933, "Tony") },
                "A solitary hitman takes in the girl from next door, and Léon's potted plant finally gets roots."),
            ("The Lion King", 1994, 88, new[] { "Animation", "Adventure" }, "English", "Roger Allers & Rob Minkoff", 45m,
                new[] { ("Matthew Broderick", 1962, "Simba (voice)"), ("James Earl Jones", 1931, "Mufasa (voice)"), ("Rowan Atkinson", 1955, "Zazu (voice)") },
                "A lion cub blamed for his father's death runs from the throne until the king in him circles back."),
            ("Se7en", 1995, 127, new[] { "Thriller", "Crime" }, "English", "David Fincher", 33m,
                new[] { ("Brad Pitt", 1963, "David Mills"), ("Morgan Freeman", 1937, "William Somerset"), ("R. Lee Ermey", 1944, "Police Captain") },
                "Two detectives trail a preacher of seven sins who plans his sermon one corpse ahead of them."),
            ("Braveheart", 1995, 178, new[] { "Drama", "War" }, "English", "Mel Gibson", 72m,
                new[] { ("Mel Gibson", 1956, "William Wallace"), ("Sophie Marceau", 1966, "Princess Isabelle"), ("Angus Macfadyen", 1963, "Robert the Bruce") },
                "A Scottish farmer loses his wife to the crown and raises a country with a braveheart and a broadsword."),
            ("Heat", 1995, 170, new[] { "Crime", "Thriller" }, "English", "Michael Mann", 60m,
                new[] { ("Al Pacino", 1940, "Vincent Hanna"), ("Robert De Niro", 1943, "Neil McCauley"), ("Tom Sizemore", 1961, "Michael Cheritto") },
                "A master thief and a restless detective share one cup of coffee and the heat of one last score."),
            ("Toy Story", 1995, 81, new[] { "Animation", "Comedy" }, "English", "John Lasseter", 30m,
                new[] { ("Tom Hanks", 1956, "Woody (voice)"), ("Tim Allen", 1953, "Buzz Lightyear (voice)"), ("Don Rickles", 1926, "Mr. Potato Head (voice)") },
                "A pull-string cowboy learns to share the toy box when a space ranger crash-lands into his story."),
            ("Twelve Monkeys", 1995, 129, new[] { "Sci-Fi", "Thriller" }, "English", "Terry Gilliam", 29m,
                new[] { ("Bruce Willis", 1955, "James Cole"), ("Madeleine Stowe", 1958, "Kathryn Railly"), ("Brad Pitt", 1963, "Jeffrey Goines") },
                "A convict is sent back to trace a plague to an army of twelve monkeys that may only be graffiti."),
            ("The Usual Suspects", 1995, 106, new[] { "Crime", "Mystery" }, "English", "Bryan Singer", 6m,
                new[] { ("Kevin Spacey", 1959, "Verbal Kint"), ("Gabriel Byrne", 1950, "Dean Keaton"), ("Pete Postlethwaite", 1946, "Kobayashi") },
                "Five usual suspects share one lineup, one job, and one story that ends with the name Keyser Söze."),
            ("Casino", 1995, 178, new[] { "Crime", "Drama" }, "English", "Martin Scorsese", 50m,
                new[] { ("Robert De Niro", 1943, "Sam Rothstein"), ("Sharon Stone", 1958, "Ginger McKenna"), ("Frank Vincent", 1937, "Frank Marino") },
                "A handicapper runs the perfect casino until love, powder and old friends comp themselves in."),
            ("Before Sunrise", 1995, 101, new[] { "Romance", "Drama" }, "English", "Richard Linklater", 2.5m,
                new[] { ("Ethan Hawke", 1970, "Jesse"), ("Julie Delpy", 1969, "Céline"), ("Andrea Eckert", 1958, "Wife on Train") },
                "Two strangers step off a train in Vienna and talk until sunrise dares them to mean it."),
            ("Fargo", 1996, 98, new[] { "Crime", "Drama" }, "English", "Joel Coen", 7m,
                new[] { ("Frances McDormand", 1957, "Marge Gunderson"), ("William H. Macy", 1950, "Jerry Lundegaard"), ("Steve Buscemi", 1957, "Carl Showalter") },
                "A car salesman rents a kidnapping he cannot afford, and a very pregnant police chief follows the snow to Fargo."),
            ("Trainspotting", 1996, 93, new[] { "Drama" }, "English", "Danny Boyle", 1.5m,
                new[] { ("Ewan McGregor", 1971, "Mark Renton"), ("Robert Carlyle", 1961, "Begbie"), ("Ewen Bremner", 1972, "Spud") },
                "Renton chooses life, then chooses otherwise, in an Edinburgh where the trains are the only thing running clean."),
            ("Life Is Beautiful", 1997, 116, new[] { "Drama", "War" }, "Italian", "Roberto Benigni", 20m,
                new[] { ("Roberto Benigni", 1952, "Guido"), ("Nicoletta Braschi", 1960, "Dora"), ("Giorgio Cantarini", 1992, "Giosuè") },
                "A father turns a camp into a points game so his son can keep believing life is beautiful."),
            ("Princess Mononoke", 1997, 134, new[] { "Animation", "Fantasy" }, "Japanese", "Hayao Miyazaki", 24m,
                new[] { ("Yōji Matsuda", 1967, "Ashitaka (voice)"), ("Yuriko Ishida", 1969, "San (voice)"), ("Kaoru Kobayashi", 1951, "Jigo (voice)") },
                "A cursed prince rides west and lands between iron town and forest gods, where the wolf princess Mononoke bites first."),
            ("L.A. Confidential", 1997, 138, new[] { "Crime", "Mystery" }, "English", "Curtis Hanson", 35m,
                new[] { ("Russell Crowe", 1964, "Bud White"), ("Kim Basinger", 1953, "Lynn Bracken"), ("Guy Pearce", 1967, "Ed Exley") },
                "Three cops who cannot stand each other pull the same confidential thread and unravel golden-age Los Angeles."),
            ("Titanic", 1997, 194, new[] { "Drama", "Romance" }, "English", "James Cameron", 200m,
                new[] { ("Leonardo DiCaprio", 1974, "Jack Dawson"), ("Kate Winslet", 1975, "Rose DeWitt Bukater"), ("Frances Fisher", 1952, "Ruth") },
                "A poor artist and a promised bride meet aboard the Titanic, which history has other plans for."),
            ("Good Will Hunting", 1997, 126, new[] { "Drama" }, "English", "Gus Van Sant", 10m,
                new[] { ("Matt Damon", 1970, "Will Hunting"), ("Robin Williams", 1951, "Sean Maguire"), ("Stellan Skarsgård", 1951, "Gerald Lambeau") },
                "A janitor at MIT solves the unsolvable for fun, and a good therapist bets he can reach Will before the fear does."),
            ("The Big Lebowski", 1998, 117, new[] { "Comedy", "Crime" }, "English", "Joel Coen", 15m,
                new[] { ("Jeff Bridges", 1949, "The Dude"), ("John Goodman", 1952, "Walter Sobchak"), ("Philip Seymour Hoffman", 1967, "Brandt") },
                "The Dude wants his rug back; what Lebowski gets instead is nihilists, a toe, and league night."),
            ("Saving Private Ryan", 1998, 169, new[] { "War", "Drama" }, "English", "Steven Spielberg", 70m,
                new[] { ("Tom Hanks", 1956, "Captain Miller"), ("Matt Damon", 1970, "Private Ryan"), ("Barry Pepper", 1970, "Private Jackson") },
                "Eight men wade out of Omaha Beach with one order: find Private Ryan and send him home alive."),
            ("The Truman Show", 1998, 103, new[] { "Drama", "Comedy" }, "English", "Peter Weir", 60m,
                new[] { ("Jim Carrey", 1962, "Truman Burbank"), ("Ed Harris", 1950, "Christof"), ("Noah Emmerich", 1965, "Marlon") },
                "Truman's whole town is a soundstage and every friend a hire — and the show only works while he never looks up."),
            ("American History X", 1998, 119, new[] { "Drama" }, "English", "Tony Kaye", 20m,
                new[] { ("Edward Norton", 1969, "Derek Vinyard"), ("Edward Furlong", 1977, "Danny Vinyard"), ("Beverly D'Angelo", 1951, "Doris Vinyard") },
                "A reformed skinhead comes home from prison to find his little brother reciting his old american history."),
            ("Fucking Åmål", 1998, 89, new[] { "Drama", "Romance" }, "Swedish", "Lukas Moodysson", 1m,
                new[] { ("Alexandra Dahlström", 1984, "Elin"), ("Rebecka Liljeberg", 1981, "Agnes"), ("Erica Carlson", 1981, "Jessica") },
                "Two girls stuck in Åmål, where nothing ever happens, make something happen to each other."),
            ("Festen", 1998, 105, new[] { "Drama" }, "Danish", "Thomas Vinterberg", 1.3m,
                new[] { ("Ulrich Thomsen", 1963, "Christian"), ("Henning Moritzen", 1928, "Helge"), ("Paprika Steen", 1964, "Helene") },
                "At his father's sixtieth birthday festen, the eldest son taps his glass and reads the wrong speech on purpose."),
            ("Run Lola Run", 1998, 80, new[] { "Thriller", "Action" }, "German", "Tom Tykwer", 2m,
                new[] { ("Franka Potente", 1974, "Lola"), ("Moritz Bleibtreu", 1971, "Manni"), ("Herbert Knaup", 1956, "Lola's Father") },
                "Lola has twenty minutes to find a hundred thousand marks, so Lola runs — three times, three endings."),
            ("The Matrix", 1999, 136, new[] { "Sci-Fi", "Action" }, "English", "The Wachowskis", 63m,
                new[] { ("Keanu Reeves", 1964, "Neo"), ("Laurence Fishburne", 1961, "Morpheus"), ("Hugo Weaving", 1960, "Agent Smith") },
                "A hacker takes the red pill and wakes from the matrix into the war his whole life was hiding."),
            ("Fight Club", 1999, 139, new[] { "Drama", "Thriller" }, "English", "David Fincher", 63m,
                new[] { ("Edward Norton", 1969, "The Narrator"), ("Brad Pitt", 1963, "Tyler Durden"), ("Meat Loaf", 1947, "Robert Paulson") },
                "An insomniac and a soap salesman open a club whose first rule everyone famously breaks."),
            ("The Sixth Sense", 1999, 107, new[] { "Thriller", "Mystery" }, "English", "M. Night Shyamalan", 40m,
                new[] { ("Bruce Willis", 1955, "Malcolm Crowe"), ("Haley Joel Osment", 1988, "Cole Sear"), ("Toni Collette", 1972, "Lynn Sear") },
                "A child psychologist takes on a boy with a sixth sense for the dead, and misses the obvious patient."),
            ("The Green Mile", 1999, 189, new[] { "Drama", "Fantasy" }, "English", "Frank Darabont", 60m,
                new[] { ("Tom Hanks", 1956, "Paul Edgecomb"), ("Michael Clarke Duncan", 1957, "John Coffey"), ("Doug Hutchison", 1960, "Percy Wetmore") },
                "On the green mile of a Depression-era death row, a gentle giant carries a gift no cell can hold."),
            ("American Beauty", 1999, 122, new[] { "Drama" }, "English", "Sam Mendes", 15m,
                new[] { ("Kevin Spacey", 1959, "Lester Burnham"), ("Annette Bening", 1958, "Carolyn Burnham"), ("Thora Birch", 1982, "Jane Burnham") },
                "A numb suburban father quits everything at once and spends his last year chasing american beauty in a plastic bag."),
            ("Being John Malkovich", 1999, 113, new[] { "Comedy", "Fantasy" }, "English", "Spike Jonze", 13m,
                new[] { ("John Cusack", 1966, "Craig Schwartz"), ("Cameron Diaz", 1972, "Lotte Schwartz"), ("Catherine Keener", 1959, "Maxine") },
                "A puppeteer finds a door on floor seven and a half that opens into being John Malkovich for fifteen minutes."),
            ("Magnolia", 1999, 188, new[] { "Drama" }, "English", "Paul Thomas Anderson", 37m,
                new[] { ("Tom Cruise", 1962, "Frank Mackey"), ("Julianne Moore", 1960, "Linda Partridge"), ("John C. Reilly", 1965, "Officer Kurring") },
                "One San Fernando day braids nine lonely lives together until the magnolia sky does something impossible."),
            ("Gladiator", 2000, 155, new[] { "Action", "Drama" }, "English", "Ridley Scott", 103m,
                new[] { ("Russell Crowe", 1964, "Maximus"), ("Joaquin Phoenix", 1974, "Commodus"), ("Djimon Hounsou", 1964, "Juba") },
                "A betrayed general returns as a gladiator and wins the crowd the emperor cannot buy."),
            ("Memento", 2000, 113, new[] { "Mystery", "Thriller" }, "English", "Christopher Nolan", 9m,
                new[] { ("Guy Pearce", 1967, "Leonard Shelby"), ("Carrie-Anne Moss", 1967, "Natalie"), ("Joe Pantoliano", 1951, "Teddy") },
                "A man who cannot make new memories hunts his wife's killer with polaroids, ink, and a memento he cannot trust."),
            ("Requiem for a Dream", 2000, 102, new[] { "Drama" }, "English", "Darren Aronofsky", 4.5m,
                new[] { ("Ellen Burstyn", 1932, "Sara Goldfarb"), ("Jared Leto", 1971, "Harry Goldfarb"), ("Marlon Wayans", 1972, "Tyrone Love") },
                "Four Coney Island dreamers chase their fixes into winter, and the requiem plays them out one by one."),
            ("Crouching Tiger, Hidden Dragon", 2000, 120, new[] { "Action", "Fantasy" }, "Mandarin", "Ang Lee", 17m,
                new[] { ("Chow Yun-fat", 1955, "Li Mu Bai"), ("Michelle Yeoh", 1962, "Yu Shu Lien"), ("Zhang Ziyi", 1979, "Jen Yu") },
                "A stolen sword sends warriors over rooftops and treetops, where every crouching tiger hides a dragon heart."),
            ("Amores Perros", 2000, 154, new[] { "Drama", "Thriller" }, "Spanish", "Alejandro González Iñárritu", 2m,
                new[] { ("Gael García Bernal", 1978, "Octavio"), ("Goya Toledo", 1969, "Valeria"), ("Emilio Echevarría", 1944, "El Chivo") },
                "One car crash in Mexico City welds together three stories of love gone feral — amores perros indeed."),
            ("Snatch", 2000, 104, new[] { "Crime", "Comedy" }, "English", "Guy Ritchie", 10m,
                new[] { ("Jason Statham", 1967, "Turkish"), ("Brad Pitt", 1963, "Mickey O'Neil"), ("Alan Ford", 1938, "Brick Top") },
                "An eighty-six carat diamond bounces between boxers, bookies and one unintelligible traveller trying to snatch it."),
            ("Tillsammans", 2000, 106, new[] { "Comedy", "Drama" }, "Swedish", "Lukas Moodysson", 2m,
                new[] { ("Lisa Lindgren", 1960, "Elisabeth"), ("Michael Nyqvist", 1960, "Rolf"), ("Gustaf Hammarsten", 1967, "Göran") },
                "A battered wife moves her kids into her brother's commune, where everyone lives tillsammans and nothing is simple."),
            ("Amélie", 2001, 122, new[] { "Comedy", "Romance" }, "French", "Jean-Pierre Jeunet", 10m,
                new[] { ("Audrey Tautou", 1976, "Amélie Poulain"), ("Mathieu Kassovitz", 1967, "Nino"), ("Serge Merlin", 1932, "Raymond Dufayel") },
                "From her Montmartre café, Amélie secretly repairs strangers' lives and nearly forgets to collect her own."),
            ("The Lord of the Rings: The Fellowship of the Ring", 2001, 178, new[] { "Fantasy", "Adventure" }, "English", "Peter Jackson", 93m,
                new[] { ("Elijah Wood", 1981, "Frodo Baggins"), ("Ian McKellen", 1939, "Gandalf"), ("Sean Bean", 1959, "Boromir") },
                "A hobbit inherits one ring, and a fellowship of nine walks it toward the only fire that can unmake it."),
            ("A Beautiful Mind", 2001, 135, new[] { "Drama" }, "English", "Ron Howard", 58m,
                new[] { ("Russell Crowe", 1964, "John Nash"), ("Jennifer Connelly", 1970, "Alicia Nash"), ("Paul Bettany", 1971, "Charles") },
                "A brilliant mathematician learns which of the rooms in his beautiful mind are real."),
            ("Spirited Away", 2001, 125, new[] { "Animation", "Fantasy" }, "Japanese", "Hayao Miyazaki", 19m,
                new[] { ("Rumi Hiiragi", 1987, "Chihiro (voice)"), ("Miyu Irino", 1988, "Haku (voice)"), ("Mari Natsuki", 1952, "Yubaba (voice)") },
                "A girl spirited away into a bathhouse for gods works to win back her name and her parents."),
            ("Donnie Darko", 2001, 113, new[] { "Sci-Fi", "Mystery" }, "English", "Richard Kelly", 4.5m,
                new[] { ("Jake Gyllenhaal", 1980, "Donnie Darko"), ("Jena Malone", 1984, "Gretchen Ross"), ("Mary McDonnell", 1952, "Rose Darko") },
                "A jet engine misses Donnie by one sleepwalk, and a rabbit named Frank starts counting down the sky."),
            ("Monsters, Inc.", 2001, 92, new[] { "Animation", "Comedy" }, "English", "Pete Docter", 115m,
                new[] { ("John Goodman", 1952, "Sulley (voice)"), ("Billy Crystal", 1948, "Mike (voice)"), ("Steve Buscemi", 1957, "Randall (voice)") },
                "The top scarer at Monsters, Inc. finds a toddler in his workflow and discovers laughter out-powers screams."),
            ("The Pianist", 2002, 150, new[] { "Drama", "War" }, "English", "Roman Polanski", 35m,
                new[] { ("Adrien Brody", 1973, "Władysław Szpilman"), ("Thomas Kretschmann", 1962, "Captain Hosenfeld"), ("Emilia Fox", 1974, "Dorota") },
                "A Warsaw pianist survives the ghetto on scraps and silence, keeping the music alive in his hands."),
            ("City of God", 2002, 130, new[] { "Crime", "Drama" }, "Portuguese", "Fernando Meirelles", 3.3m,
                new[] { ("Alexandre Rodrigues", 1983, "Rocket"), ("Leandro Firmino", 1978, "Li'l Zé"), ("Phellipe Haagensen", 1983, "Bené") },
                "In the city of God, a camera is the only weapon that ever gets Rocket out."),
            ("The Lord of the Rings: The Two Towers", 2002, 179, new[] { "Fantasy", "Adventure" }, "English", "Peter Jackson", 94m,
                new[] { ("Elijah Wood", 1981, "Frodo Baggins"), ("Ian McKellen", 1939, "Gandalf"), ("Bernard Hill", 1944, "Théoden") },
                "The fellowship is broken three ways, and between two towers an army marches on a wall of men."),
            ("Oldboy", 2003, 120, new[] { "Thriller", "Mystery" }, "Korean", "Park Chan-wook", 3m,
                new[] { ("Choi Min-sik", 1962, "Oh Dae-su"), ("Yoo Ji-tae", 1976, "Lee Woo-jin"), ("Kang Hye-jung", 1982, "Mi-do") },
                "Freed after fifteen unexplained years in a cell, an oldboy gets five days, a hammer, and a terrible answer."),
            ("Kill Bill: Vol. 1", 2003, 111, new[] { "Action", "Thriller" }, "English", "Quentin Tarantino", 30m,
                new[] { ("Uma Thurman", 1970, "The Bride"), ("Lucy Liu", 1968, "O-Ren Ishii"), ("Sonny Chiba", 1939, "Hattori Hanzō") },
                "The Bride wakes from a four-year coma, writes five names on a list, and goes to kill Bill last."),
            ("Finding Nemo", 2003, 100, new[] { "Animation", "Adventure" }, "English", "Andrew Stanton", 94m,
                new[] { ("Albert Brooks", 1947, "Marlin (voice)"), ("Ellen DeGeneres", 1958, "Dory (voice)"), ("Willem Dafoe", 1955, "Gill (voice)") },
                "An anxious clownfish crosses the ocean with a forgetful friend, finding Nemo and his own nerve."),
            ("The Lord of the Rings: The Return of the King", 2003, 201, new[] { "Fantasy", "Adventure" }, "English", "Peter Jackson", 94m,
                new[] { ("Elijah Wood", 1981, "Frodo Baggins"), ("Ian McKellen", 1939, "Gandalf"), ("David Wenham", 1965, "Faramir") },
                "The ring goes to the mountain and the king returns to a city on fire — everything ends, several times."),
            ("Eternal Sunshine of the Spotless Mind", 2004, 108, new[] { "Drama", "Romance" }, "English", "Michel Gondry", 20m,
                new[] { ("Jim Carrey", 1962, "Joel Barish"), ("Kate Winslet", 1975, "Clementine"), ("Tom Wilkinson", 1948, "Dr. Mierzwiak") },
                "Joel pays to have Clementine erased, then chases her sunshine backwards through his own spotless mind."),
            ("The Incredibles", 2004, 115, new[] { "Animation", "Action" }, "English", "Brad Bird", 92m,
                new[] { ("Craig T. Nelson", 1944, "Bob Parr (voice)"), ("Holly Hunter", 1958, "Helen Parr (voice)"), ("Samuel L. Jackson", 1948, "Frozone (voice)") },
                "A family of retired supers moonlights back into capes when a fan with a grudge makes it personal."),
            ("Så som i himmelen", 2004, 133, new[] { "Drama", "Romance" }, "Swedish", "Kay Pollak", 3m,
                new[] { ("Michael Nyqvist", 1960, "Daniel Daréus"), ("Frida Hallgren", 1974, "Lena"), ("Helen Sjöholm", 1970, "Gabriella") },
                "A burnt-out conductor comes home to lead the village choir, and for once it sounds så som i himmelen."),
            ("Downfall", 2004, 156, new[] { "Drama", "War" }, "German", "Oliver Hirschbiegel", 13.5m,
                new[] { ("Bruno Ganz", 1941, "Adolf Hitler"), ("Alexandra Maria Lara", 1978, "Traudl Junge"), ("Ulrich Matthes", 1959, "Joseph Goebbels") },
                "In the last bunker days of Berlin, a young secretary watches the downfall from the inside."),
            ("Hotel Rwanda", 2004, 121, new[] { "Drama", "War" }, "English", "Terry George", 17.5m,
                new[] { ("Don Cheadle", 1964, "Paul Rusesabagina"), ("Sophie Okonedo", 1968, "Tatiana"), ("Joaquin Phoenix", 1974, "Jack Daglish") },
                "A hotel manager trades favors, cash and courage to shelter a thousand neighbors inside the Hotel Rwanda."),
            ("Million Dollar Baby", 2004, 132, new[] { "Drama" }, "English", "Clint Eastwood", 30m,
                new[] { ("Hilary Swank", 1974, "Maggie Fitzgerald"), ("Clint Eastwood", 1930, "Frankie Dunn"), ("Morgan Freeman", 1937, "Eddie Dupris") },
                "A stubborn waitress talks a stubborn trainer into a corner, and the million dollar baby earns every round."),
            ("Shaun of the Dead", 2004, 99, new[] { "Comedy", "Horror" }, "English", "Edgar Wright", 6m,
                new[] { ("Simon Pegg", 1970, "Shaun"), ("Nick Frost", 1972, "Ed"), ("Penelope Wilton", 1946, "Barbara") },
                "Shaun finally gets his life together on the exact weekend the dead get up too."),
            ("Batman Begins", 2005, 140, new[] { "Action", "Adventure" }, "English", "Christopher Nolan", 150m,
                new[] { ("Christian Bale", 1974, "Bruce Wayne"), ("Michael Caine", 1933, "Alfred"), ("Cillian Murphy", 1976, "Scarecrow") },
                "Bruce Wayne comes home with a plan and a cave, and Gotham learns how the batman begins."),
            ("Brokeback Mountain", 2005, 134, new[] { "Drama", "Romance" }, "English", "Ang Lee", 14m,
                new[] { ("Heath Ledger", 1979, "Ennis Del Mar"), ("Jake Gyllenhaal", 1980, "Jack Twist"), ("Michelle Williams", 1980, "Alma") },
                "Two ranch hands spend one summer on Brokeback mountain and the rest of their lives circling it."),
            ("V for Vendetta", 2005, 132, new[] { "Action", "Thriller" }, "English", "James McTeigue", 54m,
                new[] { ("Natalie Portman", 1981, "Evey Hammond"), ("Hugo Weaving", 1960, "V"), ("Stephen Rea", 1946, "Inspector Finch") },
                "In a fascist Britain, a masked showman with a vendetta teaches a frightened country the fifth of November."),
            ("The Departed", 2006, 151, new[] { "Crime", "Thriller" }, "English", "Martin Scorsese", 90m,
                new[] { ("Leonardo DiCaprio", 1974, "Billy Costigan"), ("Matt Damon", 1970, "Colin Sullivan"), ("Vera Farmiga", 1973, "Madolyn") },
                "A mole in the police and a cop in the mob hunt each other, and Boston buries the departed."),
            ("The Prestige", 2006, 130, new[] { "Mystery", "Thriller" }, "English", "Christopher Nolan", 40m,
                new[] { ("Hugh Jackman", 1968, "Robert Angier"), ("Christian Bale", 1974, "Alfred Borden"), ("Rebecca Hall", 1982, "Sarah") },
                "Two magicians feud past all reason, each buying the prestige with something no act should cost."),
            ("Pan's Labyrinth", 2006, 118, new[] { "Fantasy", "War" }, "Spanish", "Guillermo del Toro", 19m,
                new[] { ("Ivana Baquero", 1994, "Ofelia"), ("Sergi López", 1965, "Captain Vidal"), ("Maribel Verdú", 1970, "Mercedes") },
                "In Franco's Spain, a girl slips from a cruel house into pan's labyrinth, where the tasks are older than the war."),
            ("The Lives of Others", 2006, 137, new[] { "Drama", "Thriller" }, "German", "Florian Henckel von Donnersmarck", 2m,
                new[] { ("Ulrich Mühe", 1953, "Gerd Wiesler"), ("Martina Gedeck", 1961, "Christa-Maria"), ("Sebastian Koch", 1962, "Georg Dreyman") },
                "A Stasi listener spends his nights inside the lives of others until one of them quietly becomes his own."),
            ("Children of Men", 2006, 109, new[] { "Sci-Fi", "Thriller" }, "English", "Alfonso Cuarón", 76m,
                new[] { ("Clive Owen", 1964, "Theo Faron"), ("Julianne Moore", 1960, "Julian"), ("Chiwetel Ejiofor", 1977, "Luke") },
                "In a world eighteen years past its last birth, one man escorts the only pregnant woman among the children of men."),
            ("Little Miss Sunshine", 2006, 101, new[] { "Comedy", "Drama" }, "English", "Jonathan Dayton & Valerie Faris", 8m,
                new[] { ("Abigail Breslin", 1996, "Olive"), ("Greg Kinnear", 1963, "Richard"), ("Alan Arkin", 1934, "Grandpa") },
                "A busted family drives a busted van cross-country so their little miss can dance like nobody rehearsed."),
            ("No Country for Old Men", 2007, 122, new[] { "Crime", "Thriller" }, "English", "Joel & Ethan Coen", 25m,
                new[] { ("Javier Bardem", 1969, "Anton Chigurh"), ("Josh Brolin", 1968, "Llewelyn Moss"), ("Kelly Macdonald", 1976, "Carla Jean") },
                "A hunter walks off with drug money and learns this is no country for old men or lucky ones."),
            ("There Will Be Blood", 2007, 158, new[] { "Drama" }, "English", "Paul Thomas Anderson", 25m,
                new[] { ("Daniel Day-Lewis", 1957, "Daniel Plainview"), ("Paul Dano", 1984, "Eli Sunday"), ("Dillon Freasier", 1996, "H.W.") },
                "An oilman drinks up a valley claim by claim, and where Plainview digs, there will be blood."),
            ("Ratatouille", 2007, 111, new[] { "Animation", "Comedy" }, "English", "Brad Bird", 150m,
                new[] { ("Patton Oswalt", 1969, "Remy (voice)"), ("Lou Romano", 1972, "Linguini (voice)"), ("Peter O'Toole", 1932, "Anton Ego (voice)") },
                "A rat who can cook steers a hopeless garbage boy to the top of Paris, one ratatouille at a time."),
            ("Into the Wild", 2007, 148, new[] { "Drama", "Adventure" }, "English", "Sean Penn", 15m,
                new[] { ("Emile Hirsch", 1985, "Christopher McCandless"), ("Catherine Keener", 1959, "Jan Burres"), ("Hal Holbrook", 1925, "Ron Franz") },
                "A new graduate burns his cards, walks into the wild, and signs his last note Alexander Supertramp."),
            ("Zodiac", 2007, 157, new[] { "Crime", "Mystery" }, "English", "David Fincher", 65m,
                new[] { ("Jake Gyllenhaal", 1980, "Robert Graysmith"), ("Robert Downey Jr.", 1965, "Paul Avery"), ("Mark Ruffalo", 1967, "Dave Toschi") },
                "A cartoonist, a reporter and a detective lose years to a killer who signs the zodiac and never stops writing."),
            ("The Dark Knight", 2008, 152, new[] { "Action", "Crime" }, "English", "Christopher Nolan", 185m,
                new[] { ("Christian Bale", 1974, "Bruce Wayne"), ("Heath Ledger", 1979, "The Joker"), ("Aaron Eckhart", 1968, "Harvey Dent") },
                "A clown with no plan and no price burns Gotham's rules, and its dark knight takes the blame to keep the light on."),
            ("WALL-E", 2008, 98, new[] { "Animation", "Sci-Fi" }, "English", "Andrew Stanton", 180m,
                new[] { ("Ben Burtt", 1948, "WALL-E (voice)"), ("Elissa Knight", 1975, "EVE (voice)"), ("Jeff Garlin", 1962, "Captain McCrea (voice)") },
                "The last little trash robot on Earth follows a sleek probe into space, carrying a plant and seven hundred years of loneliness."),
            ("Let the Right One In", 2008, 114, new[] { "Horror", "Romance" }, "Swedish", "Tomas Alfredson", 4m,
                new[] { ("Kåre Hedebrant", 1995, "Oskar"), ("Lina Leandersson", 1995, "Eli"), ("Per Ragnar", 1941, "Håkan") },
                "A bullied boy in a snowy suburb lets the right one in, and she has been twelve for a very long time."),
            ("Slumdog Millionaire", 2008, 120, new[] { "Drama", "Romance" }, "English", "Danny Boyle", 15m,
                new[] { ("Dev Patel", 1990, "Jamal Malik"), ("Freida Pinto", 1984, "Latika"), ("Anil Kapoor", 1956, "Prem Kumar") },
                "A slumdog knows every quiz answer because every question already cost him something."),
            ("In Bruges", 2008, 107, new[] { "Comedy", "Crime" }, "English", "Martin McDonagh", 15m,
                new[] { ("Colin Farrell", 1976, "Ray"), ("Brendan Gleeson", 1955, "Ken"), ("Ralph Fiennes", 1962, "Harry") },
                "Two hitmen are sent to lie low in Bruges, which one of them decides is exactly what purgatory looks like."),
            ("Gran Torino", 2008, 116, new[] { "Drama" }, "English", "Clint Eastwood", 33m,
                new[] { ("Clint Eastwood", 1930, "Walt Kowalski"), ("Bee Vang", 1991, "Thao"), ("Ahney Her", 1992, "Sue") },
                "A growling widower guards his gran torino from the neighborhood until the neighborhood becomes his."),
            ("Inglourious Basterds", 2009, 153, new[] { "War", "Drama" }, "English", "Quentin Tarantino", 70m,
                new[] { ("Brad Pitt", 1963, "Aldo Raine"), ("Christoph Waltz", 1956, "Hans Landa"), ("Mélanie Laurent", 1983, "Shosanna") },
                "A squad of basterds and a cinema owner separately plan the same movie premiere to end the war early."),
            ("Up", 2009, 96, new[] { "Animation", "Adventure" }, "English", "Pete Docter", 175m,
                new[] { ("Ed Asner", 1929, "Carl Fredricksen (voice)"), ("Jordan Nagai", 2000, "Russell (voice)"), ("Christopher Plummer", 1929, "Charles Muntz (voice)") },
                "A widower ties ten thousand balloons to his house and goes up, unaware of the scout on his porch."),
            ("District 9", 2009, 112, new[] { "Sci-Fi", "Thriller" }, "English", "Neill Blomkamp", 30m,
                new[] { ("Sharlto Copley", 1973, "Wikus van de Merwe"), ("Jason Cope", 1973, "Christopher Johnson"), ("Vanessa Haywood", 1979, "Tania") },
                "A cheerful bureaucrat serving eviction notices in district 9 starts turning into what he is evicting."),
            ("Avatar", 2009, 162, new[] { "Sci-Fi", "Adventure" }, "English", "James Cameron", 237m,
                new[] { ("Sam Worthington", 1976, "Jake Sully"), ("Zoe Saldana", 1978, "Neytiri"), ("Sigourney Weaver", 1949, "Dr. Grace Augustine") },
                "A paraplegic marine walks again inside an avatar and has to choose which of his two bodies is home.")
        };

        foreach (var film in wall)
        {
            var movie = new Movie { Title = film.Title, Year = film.Year, Duration = film.Min };
            foreach (var name in film.Genres) movie.Genres.Add(genreByName[name]);
            for (var billing = 0; billing < film.Cast.Length; billing++)
            {
                var (name, born, role) = film.Cast[billing];
                movie.Cast.Add(new MovieActor
                    { Actor = ActorOf(name, born), Role = role, Billing = billing });
            }
            movie.Details = new MovieDetails
            {
                Synopsis = film.Synopsis,
                Language = film.Language,
                Director = film.Director,
                Budget = film.BudgetM * 1_000_000m
            };
            context.Movies.Add(movie);
        }

        context.SaveChanges();
    }
}