using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class StudentPerformanceRepository : IStudentPerformanceRepository
    {
        private readonly Context _context;

        public StudentPerformanceRepository(Context context)
        {
            _context = context;
        }

        public async Task<StudentPerformance?> GetByStudentSubjectClassAsync(int studentId, int subjectId, string academicYear, CancellationToken ct = default)
        {
            return await _context.StudentPerformances
                .FirstOrDefaultAsync(sp => sp.StudentId == studentId && 
                                           sp.SubjectId == subjectId && 
                                           sp.AcademicYear == academicYear, ct);
        }

        public async Task<IEnumerable<StudentPerformance>> GetByStudentAndYearAsync(int studentId, string academicYear, CancellationToken ct = default)
        {
            return await _context.StudentPerformances
                .Include(sp => sp.Subject)
                .Where(sp => sp.StudentId == studentId && sp.AcademicYear == academicYear)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<StudentPerformance>> GetByClassAndSubjectAsync(int classId, int subjectId, string academicYear, CancellationToken ct = default)
        {
            return await _context.StudentPerformances
                .Include(sp => sp.Student)
                .Where(sp => sp.ClassId == classId && sp.SubjectId == subjectId && sp.AcademicYear == academicYear)
                .ToListAsync(ct);
        }

        public async Task UpsertAsync(StudentPerformance performance, CancellationToken ct = default)
        {
            var existing = await _context.StudentPerformances
                .FirstOrDefaultAsync(sp => sp.StudentId == performance.StudentId && 
                                           sp.SubjectId == performance.SubjectId && 
                                           sp.AcademicYear == performance.AcademicYear, ct);

            if (existing == null)
            {
                await _context.StudentPerformances.AddAsync(performance, ct);
            }
            else
            {
                existing.AvgAssignment = performance.AvgAssignment;
                existing.AvgExam = performance.AvgExam;
                existing.TotalExamsTaken = performance.TotalExamsTaken;
                existing.TotalAssignmentsSubmitted = performance.TotalAssignmentsSubmitted;
                existing.TotalAssignmentsPending = performance.TotalAssignmentsPending;
                existing.UpdatedAt = performance.UpdatedAt;
                existing.ClassId = performance.ClassId;
                
                _context.StudentPerformances.Update(existing);
            }

            await _context.SaveChangesAsync(ct);
        }
    }
}
