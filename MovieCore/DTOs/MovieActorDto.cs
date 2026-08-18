namespace MovieCore.DTOs
{
  // An actor as seen from inside ine movie: the actor's own field 
  // plus the role, which lives on the relationship
  public class MovieActorDto
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BirthYear { get; set; }
    public string? Role { get; set; }
  }
}