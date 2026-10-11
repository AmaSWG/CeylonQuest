# Platform reviews

## Eligibility and permissions

Active authenticated Visitor and Provider accounts may submit reviews.
Accounts awaiting activation cannot submit.
Guests and Admin accounts cannot submit.
A booking or completed payment is not required.
Multiple reviews per eligible account are permitted.
Retrieval is public.

## Validation and storage

Rating must be an integer from 1 through 5.
Content is required and must contain 1–2,000 characters after trimming.
The authenticated identity determines the stored user ID.
The server generates the review ID and UTC creation timestamp.

## Retrieval and display

Visitor and Provider reviews appear in one collection.
Reviews are ordered by creation timestamp descending, then ID descending.
The rating filter matches exactly and is applied before pagination.
The default page size is 20; the maximum is 100.

Responses expose reviewer display name, available profile picture,
rating, content, and creation timestamp.
Email addresses and phone numbers are not exposed.

The homepage displays up to three latest reviews.
View More opens /?page=platform-reviews.
The dedicated page supports all ratings or an exact 1–5 rating filter.
Dates are displayed in the Asia/Colombo timezone.
Review content is rendered as plain text.

## Verification

Run the IdentityService test project.
Run the frontend build and lint checks.
Verify real HTTP authentication and authorization.
Complete manual QA and code review before closing the task.