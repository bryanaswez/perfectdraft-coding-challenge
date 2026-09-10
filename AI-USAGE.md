# AI Usage

I used AI assistance for this exercise. This file describes what I used, what I asked for, what it
produced, and how I checked it.

## Summary

I worked through the exercise in stages rather than asking for a finished solution. I rejected the
first implementation as over engineered and had it cut from 379 lines to 121. I drove the second
half test first, writing failing specifications for each defect before any fix was made. Where the
assistant asserted that a rule was enforced, I asked to be shown where, and found in one case that
it was not enforced at all. Every failure mode was reproduced against a running instance rather than
assumed from the test output. The design decisions in this solution are mine, and in several cases
I chose against the assistant's initial suggestion.

## Tools

- Claude Code, from Anthropic.
- Model: Claude Haiku 4.5.

## What I asked for

A summary of the instructions I gave, in the order I gave them.

**Understanding the problem before writing anything**

- Read the repository and tell me what is here.
- Do not write anything yet. Ask me questions about the exercise so I can check my understanding,
  then explain the answers, covering why there are two upstream systems, why Magento is the source
  of truth, why search only products must be excluded, and which status codes are appropriate.
- Sketch a class structure before any code is written.

**Building it in small steps**

- Start with step one only, which is reading the data files and proving the project can reach them.
- Why did you add a `JsonPropertyName` attribute only for `objectID`?
- Clean up the code I wrote, without changing behaviour.
- Implement the search endpoint with these decisions: no term returns everything, no matches returns
  200 with an empty list.
- Add enrichment from the search index, then derive an `inStock` boolean and do not expose the stock
  number.
- Write the tests before we proceed.

**Finding what was missing**

- Go through the README requirements and check what is missing.
- Is the only purpose of the search file to supply an image? I thought it existed for search
  performance.
- List the limitations in order of importance.

**Fixing it test first**

- Write tests that demonstrate the defects. They should fail, and I will make the changes so they
  pass.
- Fix the index degradation, then the duplicate and null SKU handling and case insensitive ids, then
  SKU search, then the 503 and ProblemDetails response.
- Explain the changes. Why does this function fetch all the search products?
- Bring `NOTES.md` and `AI-USAGE.md` up to date.

## My decisions

The following were my decisions, not the assistant's. In several cases I chose against its initial
suggestion, or asked it to justify one before accepting it:

- Returning 200 with an empty array rather than 204 for a search with no matches. I proposed 204 and
  changed my mind after asking for the argument against it.
- Returning the full collection for a request with no search term.
- Exposing a derived `inStock` boolean and removing the stock quantity from the response.
- Supporting SKU search, which the assistant had flagged as an open question either way.
- Rejecting the first implementation as over engineered and requiring it to be cut back.
- Removing the redundant `JsonPropertyName` attribute.
- Requiring the defect tests to be written as failing specifications rather than as tests that
  documented the broken behaviour and passed.

I also wrote the first working version of the search endpoint myself, and wired the controller to
`ProductDataFiles` myself.

## Which parts were written by the assistant

Most of the code in the final solution was written by the assistant to my instructions:

- `Upstreams/MagentoProduct.cs`, `Upstreams/SearchProduct.cs`, `Upstreams/ProductDataFiles.cs`,
  `Upstreams/UpstreamUnavailableException.cs`
- `Contracts/ProductResponse.cs`, including the merge rules in `From`
- `Infrastructure/UpstreamExceptionHandler.cs`
- `Controllers/ProductController.cs` in its final form, and the wiring in `Program.cs`
- The `Content` item in the API `.csproj` that copies the data files
- All three test files, the broken fixtures in `TestData`, and the test project package references
- This file and `NOTES.md`

## How I reviewed, tested and changed the output

**I read every file and cut the first pass down.** The initial implementation of the data reading
step was 379 lines across ten files, including interfaces, a caching layer, deduplication and
warning logs that I had not asked for and that nothing yet needed. I had it stripped to 121 lines
across four files. Some of that work was reintroduced later, but only once a failing test required
it.

**I challenged code I did not understand.** I questioned why `SearchProduct` carried a
`JsonPropertyName` attribute when the other properties did not. It turned out to be redundant,
because the serialiser is configured to match property names case insensitively, so it was removed
and the mapping was verified against the real file afterwards.

**I asked where rules were actually enforced.** When told the index price and stock were discarded,
I asked to be shown where. Nothing discarded them, they were simply never read, which meant the
central rule of the exercise was guaranteed by nothing. That is why the merge rules now have unit
tests.

**I drove the second half with tests first.** Once the gaps were identified, I asked for them to be
written as failing tests stating the intended behaviour, and then fixed the code until they passed.
The suite went from eight failing to zero across five changes, and each change was verified by the
test run rather than by assertion. Two of those specifications encoded decisions the assistant
flagged as mine to make, and I set them before any code was written.

**I tested manually as I went.** Each stage was run and exercised through Postman before moving on,
which is how I confirmed the data files were being read, that enrichment appeared for products with
an index row, and that `M100` returned a null image rather than failing.

**Failure modes were verified rather than assumed.** The upstream failures were reproduced by
overriding the configured file paths to point at missing and malformed files, and by feeding in rows
with null keys and duplicate SKUs. The 503 and its `application/problem+json` response were checked
against a running instance, not only through the test suite.

**Reverting.** On a few occasions the assistant's edits went further than I wanted, and I reverted
the file and had the change reapplied more narrowly.

**Corrections.** The assistant was wrong twice in ways the tests caught. It predicted the test host
would rethrow upstream exceptions when it in fact returns a 500 response, and it understated the
malformed data defect, having assumed an empty body where the developer exception page actually
returns a full stack trace. Both were found by running the tests rather than by reading the output.

## A note on ownership

I can explain any decision or any line in this solution, including the parts I did not type, and I
am happy to walk through any of it.
