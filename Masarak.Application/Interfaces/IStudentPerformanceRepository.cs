using Masarak.Domain.Entities;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Repository for aggregated StudentPerformance records.
    /// </summary>
    public interface IStudentPerformanceRepository
    {
        Task<StudentPerformance?> GetByStudentSubjectClassAsync(int studentId, int subjectId, string academicYear, CancellationToken ct = default);
        Task<IEnumerable<StudentPerformance>> GetByStudentAndYearAsync(int studentId, string academicYear, CancellationToken ct = default);
        Task<IEnumerable<StudentPerformance>> GetByClassAndSubjectAsync(int classId, int subjectId, string academicYear, CancellationToken ct = default);
        Task UpsertAsync(StudentPerformance performance, CancellationToken ct = default);
    }
}
