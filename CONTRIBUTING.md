# Contributing to Simple Shell

We love your input! We want to make contributing to Simple Shell as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Becoming a maintainer

## We Develop with Github
We use Github to host code, to track issues and feature requests, as well as accept pull requests.

## We Use [Github Flow](https://guides.github.com/introduction/flow/index.html)
Pull requests are the best way to propose changes to the codebase. We actively welcome your pull requests:

1. Fork the repo and create your branch from `master`.
2. If you've added code that should be tested, add tests.
3. If you've changed APIs, update the documentation.
4. Ensure the test suite passes.
5. Make sure your code follows our coding standards.
6. Issue that pull request!

## Any contributions you make will be under the MIT Software License
In short, when you submit code changes, your submissions are understood to be under the same [MIT License](http://choosealicense.com/licenses/mit/) that covers the project. Feel free to contact the maintainers if that's a concern.

## Report bugs using Github's [issue tracker](https://github.com/OmarAglan/simple_shell/issues)
We use GitHub issues to track public bugs. Report a bug by [opening a new issue](https://github.com/OmarAglan/simple_shell/issues/new); it's that easy!

## Write bug reports with detail, background, and sample code

**Great Bug Reports** tend to have:

- A quick summary and/or background
- Steps to reproduce
  - Be specific!
  - Give sample code if you can.
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)

## Use a Consistent Coding Style

* Use 4 spaces for indentation rather than tabs
* All C files must follow the Betty style guide
* Keep functions short and focused
* Use descriptive variable names
* Comment your code when necessary

## Cross-Platform Development Guidelines

When contributing code that affects platform-specific functionality:

1. Use preprocessor directives to handle platform differences:
```c
#ifdef WINDOWS
    // Windows-specific code
#else
    // Unix/Linux code
#endif
```

2. Keep platform-specific code isolated in dedicated functions or modules
3. Test your changes on both Windows and Unix/Linux platforms
4. Document any platform-specific behaviors or requirements

## License
By contributing, you agree that your contributions will be licensed under its MIT License.
