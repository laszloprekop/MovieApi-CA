namespace MovieCore.DTOs
{
  // Body of POST /api/movies/{movieId}/actors/{actorId} - the ids travel in the route;
  // the body carries only what the route cannot
    public class ActorRoleDto
    {
        public string? Role { get; set; }
    }
}