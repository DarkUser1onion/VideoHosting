# Tasks for Like/Dislike Toggle Fix

## Phase 1: Bug Reproduction & Understanding

- [x] 1.1 Write unit test reproducing toggle bug in InteractionService
  - Create test that simulates creating a like, then toggling it off
  - Verify current behavior returns null from SetLikeAsync
  - Mark as PBT task for exploration

- [x] 1.2 Write unit test for LikesController toggle scenario
  - Create test that calls SetLike endpoint with toggle scenario
  - Verify current behavior returns 404 NotFound
  - Document expected vs actual behavior

## Phase 2: Server-Side Fix

- [x] 2.1 Fix LikesController.SetLike endpoint
  - Changed: `if (like == null) return NotFound();` → `if (like == null) return Ok(new { removed = true });`
  - Now returns 200 OK when toggle removes a like/dislike
  - Minimal fix approach - no need for additional types

- [x] 2.2 Update LikesController tests to verify fix
  - Updated `SetLike_TogglingOffExistingLike_ReturnsNotFound_BUG` → `SetLike_TogglingOffExistingLike_ReturnsOkWithRemovedTrue`
  - Updated `SetLike_TogglingOffExistingDislike_ReturnsNotFound_BUG` → `SetLike_TogglingOffExistingDislike_ReturnsOkWithRemovedTrue`
  - Tests now verify correct 200 OK response with removed=true

## Phase 3: Client-Side Fix

- [x] 3.1 Verify ApiService.SetLikeAsync handles toggle correctly
  - Current implementation already handles 200 OK with any content as success
  - Empty response or response with "removed" both return true (success)

- [x] 3.2 Verify PlayerViewModel.SetLike handles toggle correctly
  - Current implementation already re-fetches like status after success
  - UserLiked is updated via GetLikeStatusAsync which returns null when no like exists
  - IsLikeActive and IsDislikeActive computed properties work correctly

## Phase 4: Testing & Verification

- [x] 4.1 Unit tests for InteractionService.SetLikeAsync (already existed)
  - Test: Create new like returns LikeDto
  - Test: Switch like to dislike returns updated LikeDto
  - Test: Toggle off like returns null
  - Test: Toggle off dislike returns null

- [x] 4.2 Unit tests for LikesController.SetLike (fixed and verified)
  - Test: Toggle off scenario returns 200 OK with removed=true (FIXED)
  - Test: Created scenario returns 200 OK with LikeDto
  - Test: Updated scenario returns 200 OK with LikeDto

- [x] 4.3 Fixed view count increment issue
  - Removed IncrementViewsAsync from GetVideo endpoint
  - Views now only increment on actual video playback (watch endpoint)
  - Likes/dislikes no longer affect view count

- [x] 4.4 Fixed visual feedback for like/dislike buttons
  - Added BoolToActiveColorConverter for blue highlight on active buttons
  - Changed from opacity-based to color-based indication
  - Active like/dislike now shows blue color (#3B82F6)

- [ ] 4.5 Manual testing in UI
  - Test toggle like on/off
  - Test toggle dislike on/off
  - Test switch between like and dislike
  - Verify visual feedback on buttons (blue color)
  - Verify counter updates
  - Verify views don't increase on like/dislike

## Phase 5: Documentation & Cleanup

- [x] 5.1 Fix implemented and tested
  - Server returns 200 OK with `{ removed: true }` when toggle removes a like/dislike
  - Client already handles this correctly
  - All unit tests pass (10/10)

- [x] 5.2 Build verification
  - Server builds successfully
  - Client builds successfully

## Task Dependencies

```
Phase 1 (Reproduction) can run in parallel with investigation
Phase 2 (Server Fix) depends on Phase 1 completion
Phase 3 (Client Fix) depends on Phase 2 completion
Phase 4 (Testing) depends on Phase 2 and 3 completion
Phase 5 (Cleanup) depends on Phase 4 completion
```

## Notes

- Minimal fix approach: Start with just changing the controller response, only add new types if needed
- Property-based tests should verify both bug fix and preservation requirements
- Manual UI testing is essential to verify visual feedback works correctly
