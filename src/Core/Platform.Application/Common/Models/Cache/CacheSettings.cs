namespace Platform.Application.Common.Models.Cache;

/// <summary>
/// Typed options for per-entity TTL configuration.
/// Bound from appsettings.json "Cache" section.
/// </summary>
public sealed class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>Default L1 (memory) TTL when not overridden per-entity.</summary>
    public TimeSpan DefaultL1Duration { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Default L2 (Redis) TTL when not overridden per-entity.</summary>
    public TimeSpan DefaultL2Duration { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// L1 duration = L2 duration × L1ToL2Ratio.
    /// Default 0.2 → L1 is 20% of L2 duration.
    /// </summary>
    public double L1ToL2Ratio { get; init; } = 0.2;

    // ── Stampede Protection ───────────────────────────────────────────────────

    /// <summary>
    /// Maximum time to wait for lock acquisition.
    /// Should be proportional to worst-case factory duration.
    /// </summary>
    /// <remarks>5s default, increase for long-running queries (e.g., reports: 30s).</remarks>
    public TimeSpan LockTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// TTL for distributed locks. Lock holder crash → automatic release.
    /// </summary>
    /// <remarks>Should exceed worst-case factory duration.</remarks>
    public TimeSpan DistributedLockTtl { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable distributed locking for stampede protection.
    /// Set to false for single-instance deployments.
    /// </summary>
    public bool EnableDistributedLocking { get; init; } = true;

    // ── L1 Synchronization ────────────────────────────────────────────────────

    /// <summary>
    /// Redis Pub/Sub channel name for cache invalidation messages.
    /// </summary>
    public string InvalidationChannelName { get; init; } = "cache:invalidation";

    /// <summary>
    /// Enable L1 cache synchronization across multiple instances.
    /// Requires Redis Pub/Sub.
    /// </summary>
    public bool EnableL1Synchronization { get; init; } = true;

    // ── Serialization ──────────────────────────────────────────────────────────

    /// <summary>
    /// Cache serializer mode.
    /// </summary>
    public CacheSerializerMode SerializerMode { get; init; } = CacheSerializerMode.JsonOnly;

    // ── Tag-Based Invalidation ────────────────────────────────────────────────

    /// <summary>
    /// Enable tag-based invalidation for efficient bulk key deletion.
    /// Falls back to SCAN when disabled or when no tags are associated.
    /// </summary>
    public bool EnableTagBasedInvalidation { get; init; } = true;

    /// <summary>
    /// TTL for tag SET entries in Redis.
    /// Tags are refreshed when keys are associated.
    /// </summary>
    public TimeSpan TagExpiration { get; init; } = TimeSpan.FromHours(24);

    // ── Per-entity L2 TTLs ─────────────────────────────────────────────────

    public TimeSpan CategoryTreeL2      { get; init; } = TimeSpan.FromHours(2);
    public TimeSpan CategoryListL2      { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan CategoryByIdL2      { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan CategoryBySlugL2    { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan CourseListL2        { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan CourseByIdL2        { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan CourseBySlugL2      { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan CourseByInstructorL2 { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan LessonByCourseL2    { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan LessonByIdL2        { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan BadgeListL2         { get; init; } = TimeSpan.FromHours(1);
    public TimeSpan BadgeByIdL2         { get; init; } = TimeSpan.FromHours(1);
    public TimeSpan CouponByCodeL2      { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan CouponByIdL2        { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan CouponListL2        { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan EnrollmentByUserL2  { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan EnrollmentByCourseL2 { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan SubmissionByStudentL2 { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan CertificateByUserL2 { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan LiveSessionByIdL2   { get; init; } = TimeSpan.FromMinutes(3);
}

/// <summary>
/// Cache serialization mode.
/// </summary>
public enum CacheSerializerMode
{
    /// <summary>
    /// JSON serialization only (backward compatible).
    /// </summary>
    JsonOnly,

    /// <summary>
    /// MessagePack serialization only (better performance).
    /// </summary>
    MessagePackOnly,

    /// <summary>
    /// Migration mode: write MessagePack, read both JSON and MessagePack.
    /// Allows gradual migration without cache invalidation.
    /// </summary>
    JsonReadMessagePackWrite
}
