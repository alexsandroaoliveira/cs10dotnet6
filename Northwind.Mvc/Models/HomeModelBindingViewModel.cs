namespace Northwind.Mvc.Models;

public record HomeModelBindingViewModel
(
    Thing Thing,
    bool HasErros,
    IEnumerable<string> ValidationErrors
);
