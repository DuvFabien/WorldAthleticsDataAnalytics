namespace WorldAthleticsScrapper
{
    record AthletePerformance(TimeOnly Mark, int AthleteId, string Venue, DateOnly Date, string DisciplineCode);
    record Athlete(int Id, string FullName, DateOnly? DateOfBirth, string Nationality);
    record ScrappingData(List<Athlete> Athletes, List<AthletePerformance> AthletePerformances);
    public record Discipline(string Name, string Code, string Category);
}
