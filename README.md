# Sofia
Sofia is a reviewer recommender. It helps you with selecting best code reviewers to maximize knowledge **_spreading_** and **_retention_**. 

Assigning experts to pull requests ensures high code quality and helps with catching bugs before escaping to production. Studies have shown that picking an expert to review a change is trivial in most of the times.

There are other overlooked benefits that pull requests offer to teams such as spreading and distributing knowledge accross team members. As a team starts knowledge transferring through pull requests, the knowledge concentration decreases which means more developers become aware of different parts of the code. Having backup developers who have enough knowledge about code's internal design and structure make the project resiilient to turnover which is a risk inherent in software projects. 

Sofia, is a reviewer recommender that promote knowledge transferring through code reviews. Our study shows, following Sofia's suggestions in selecting reviewers could drastically reduce the risk of turnover. Sofia compiles a list of reviewers according to their previous contributions to the project, the knowledge they have about the change and the knowledge they gain by reviewing the change. Sofia gives higher priority to developers who are unlikely to leave the project in order to retains the knowledge in the team for a longer time.

## Experts vs Learners

Experts are always necessary to review a pull request to keep the quality of code. Our work suggests that teams can reap the benefits of code review like never before by using it to increase team awareness and spread knowledge. According to our findings, assigning a learner in addition to experts can be extremely effective in reducing the risk of turnover. 

# [Installation](https://github.com/apps/sofiarec)

Sofia is a GitHub application. First you need to [install](https://github.com/apps/sofiarec) it on your repository or organizational account.

## Scanning

After installing sofia, you need to ask Sofia to scan your repository. During scanning:

 1. Sofia clones your repository to gather commits and all changes.
 2. Sofia fetch all of the merged pull requests and reviewers.
 3. Using commits and reviewers, Sofia builds the knowledge model of your project.
 
After scanning the project, Sofia keeps the model up to date using GitHub WebHooks to analyze events in real time.

### Ask for Scanning

You need to open an Issue and make a comment on it like below. Instead of **master**, specify your main branch.

```
@SofiaRec scan branch master
```

A full scan usually takes around 2-3 hours for large repositories. Upon completion, Sofia lets you know by leaving a comment.

# Get Suggestions

# Spreading & Retention
