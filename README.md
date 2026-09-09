# Product Aggregation API Coding Challenge

## Task

Build a .NET 8 API that returns a unified product view from two file-backed upstream systems.

This exercise is intentionally small and open-ended. We want to see how you model the problem, make tradeoffs, and explain your decisions.

Complete the API so it supports:

- `GET /products/{id}`
- `GET /products?search={term}`

The existing endpoints return `501 Not Implemented`. Replace them with a working product aggregation implementation.

Expected behaviour:

- Combine Magento and search data into one product response model.
- Treat Magento as the source of truth when fields overlap, especially for price, currency, and stock.
- Use search data only to enrich Magento products, for example with images or indexed display content.
- Do not return products that only exist in the search index.
- Handle missing, partial, or inconsistent upstream data gracefully.
- Use appropriate HTTP status codes for success and missing products.

We deliberately do not prescribe the exact response DTO or internal design. Choose a shape you think is suitable and explain any important assumptions in your submission notes.

## GitHub Classroom Submission

You should complete your work directly in this repo.

You may use branches while developing your solution, but ensure your final work is merged into the main branch before the deadline.

There is no separate zip upload step. We will assess the state of your GitHub Classroom repository, normally the `main` branch, at the deadline.

Before you finish, check that:

- Your latest commits are visible on GitHub.
- The `main` branch contains your final solution.

## Submission Notes

Add a short `NOTES.md` file covering:

- What you changed.
- Key tradeoffs or assumptions.
- Anything you did not complete.
- What you would do next with more time.

## AI Usage

AI tooling is allowed, but you must disclose it if you use it.

If you use AI assistance, add an `AI-USAGE.md` file covering:

- The tool names, and model names if known.
- The prompts or instructions you gave, or a concise summary of them.
- Which parts of the solution were influenced by AI output.
- How you reviewed, tested, or changed the AI output before submitting.

If you do not use AI, no `AI-USAGE.md` file is required.
