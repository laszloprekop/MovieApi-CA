namespace MovieCore.Models
{
  // The N:M join promoted to a real entity: the role belongs to the relationship itself, not to the movie or the actor.
    public class MovieActor
    {
        public int ActorId { get; set; }
        public Actor Actor { get; set; } = null!;
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;
        public string? Role { get; set; }

        // Billing order within the movie's cast (0 = top billing). The quiz's
        // free clue leaks the LAST name, so this order is a domain fact, not
        // presentation. Hand-built seed rows may leave it 0 and fall back to
        // ActorId order.
        public int Billing { get; set; }
    }
}