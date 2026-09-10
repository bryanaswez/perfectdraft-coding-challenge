# Submission Notes

## What I changed

Both endpoints are implemented and the 501 stubs are gone. `GET /products/{id}` returns one
aggregated product or a 404. `GET /products?search={term}` returns a list, filtered on name or SKU.

The upstream shapes and the file reader are in `Upstreams`. The response model and the merge rules
are in `Contracts/ProductResponse.cs`. There are 22 tests, and the eight covering upstream failure
and inconsistent data were written as failing tests first.

## Decisions

**Magento decides what exists.** Both endpoints read Magento and only look sideways at the index, so
`P999` never appears. The shape of the code excludes it. There is no filter to remember.

**Magento wins on price, currency and name.** `inStock` comes from `stock > 0`. The index has its
own price and stock flag and neither is read. I left both on `SearchProduct` so the conflict stays
visible. For `P100` the index says 42.79 and out of stock, and the API returns 38.50 and in stock.

**The two upstreams are not equally important.** The index only supplies images, so a failure there
is logged and the request carries on. Magento failing means there is no catalogue, so that returns a
503 with a ProblemDetails body. File paths and stack traces go to the log, not to the caller.

**Bad rows are dropped when the file is read.** No SKU means the product cannot be ordered, so it is
not served. On a duplicate SKU the first row wins. Having a rule matters more than which rule it is.

**The response carries a boolean, not a stock count.** Stock levels are commercially sensitive on a
public endpoint.

**Search matches the name on a substring and the SKU exactly, and does not query the index.** `M100`
is in Magento with no index row, and searching the index would leave a real product unfindable.

**Status codes.** 200 for a product, 404 when it is missing or exists only in the index, and 200
with an empty array for a search with no matches. An empty result is still a successful search. No
term returns everything.

## Not done

**No caching.** Both files are read and parsed on every request, and the enrichment join scans the
index once per matched product. Not worth fixing at six products. It is the first thing I would
change for a real catalogue.

**Search is a substring match.** No tokenising, ranking or paging, so `stella keg` finds nothing.

**Smaller things.** The index `title` is unused, `sku` and `name` are nullable on the response, and
there is no OpenAPI document.

## What I would do next

1. Load each file once into a snapshot keyed by SKU, which fixes the per request parse, the linear
   scans and the join together.
2. Put the two upstreams behind separate interfaces. Their failure policies already differ.
3. Give search real matching, a defined sort order and paging.
4. Publish an OpenAPI document and make `sku` and `name` non nullable.
