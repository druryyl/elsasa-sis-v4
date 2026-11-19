import { describe, expect, it } from 'vitest';
import { announcementSchema, ANNOUNCEMENT_AUDIENCES } from '../announcement.schema';

describe('announcementSchema', () => {
  const validData = {
    title: 'Mid-Term Exam Schedule Released',
    targetAudience: 'ALL' as const,
    content: 'Please find the schedule for the upcoming mid-term examinations.',
    academicYearId: '01900000-0000-7000-8000-000000000101'
  };

  it('validates a complete valid announcement payload', () => {
    const result = announcementSchema.safeParse(validData);
    expect(result.success).toBe(true);
    if (result.success) {
      expect(result.data.title).toBe(validData.title);
      expect(result.data.targetAudience).toBe('ALL');
      expect(result.data.content).toBe(validData.content);
      expect(result.data.academicYearId).toBe(validData.academicYearId);
    }
  });

  it('allows null or omitted academicYearId', () => {
    const withoutYear = {
      title: 'General Update',
      targetAudience: 'TEACHERS' as const,
      content: 'Faculty meeting at 3 PM.'
    };
    const result = announcementSchema.safeParse(withoutYear);
    expect(result.success).toBe(true);

    const withNullYear = {
      ...withoutYear,
      academicYearId: null
    };
    const resultNull = announcementSchema.safeParse(withNullYear);
    expect(resultNull.success).toBe(true);
  });

  it('rejects an invalid academicYearId format', () => {
    const invalidYear = {
      ...validData,
      academicYearId: 'not-a-valid-uuid'
    };
    const result = announcementSchema.safeParse(invalidYear);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe('Invalid academic year ID');
    }
  });

  it('rejects empty or whitespace-only title', () => {
    const emptyTitle = { ...validData, title: '   ' };
    const result = announcementSchema.safeParse(emptyTitle);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe('Title is required');
    }
  });

  it('rejects title longer than 255 characters', () => {
    const longTitle = { ...validData, title: 'A'.repeat(256) };
    const result = announcementSchema.safeParse(longTitle);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe('Title must not exceed 255 characters');
    }
  });

  it('rejects empty or whitespace-only content', () => {
    const emptyContent = { ...validData, content: '   ' };
    const result = announcementSchema.safeParse(emptyContent);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe('Content is required');
    }
  });

  it('accepts all defined audiences', () => {
    for (const audience of ANNOUNCEMENT_AUDIENCES) {
      const data = { ...validData, targetAudience: audience };
      const result = announcementSchema.safeParse(data);
      expect(result.success).toBe(true);
    }
  });

  it('rejects invalid audience value', () => {
    const invalidAudience = { ...validData, targetAudience: 'ADMINS' };
    const result = announcementSchema.safeParse(invalidAudience);
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe('Please select a target audience');
    }
  });
});
