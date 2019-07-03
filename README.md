# Sofia
Sofia is a reviewer recommender. It helps you with selecting the best code reviewers to maximize **_knowledge  spreading_** and **_knowledge retention_**. 

Assigning experts to pull requests ensures high code quality and helps with catching bugs before escaping to production. Studies have shown that finding an expert to review a change is trivial in most of the times because code has strong ownership.

There are other overlooked benefits that pull requests offer to teams such as spreading and distributing knowledge across team members. As a team starts knowledge transferring through pull requests, the knowledge concentration decreases which means more developers become aware of different parts of the code. Having backup developers who have enough knowledge about code's internal design and structure make the project resilient to turnover which is a risk inherent in software projects.

Sofia is a reviewer recommender that promotes knowledge transferring through code reviews. Our study shows, following Sofia's suggestions in selecting reviewers could drastically reduce the risk of turnover. Sofia compiles a list of reviewers according to their previous contributions to the project, the knowledge they have about the change and the knowledge they gain by reviewing the change. Sofia gives higher priority to developers who are unlikely to leave the project in order to retain the knowledge in the team for a longer time.

## Experts vs Learners

**Experts** are always necessary to review a pull request to keep the quality of code. Our work suggests that teams can reap the benefits of code review like never before by using it to increase team awareness and spread knowledge. According to our findings, assigning a **Learner** in addition to experts can be extremely effective in reducing the risk of turnover. Learners are not experts but have enough expertise to review the code and give valuable feedback. Assigning learners to a pull request, give a learning opportunity to reviewers.

Sofia is not an expert recommender. Sofia helps teams to spread knowledge by best learners.  To retain the knowledge in the team, our heuristic suggests learners who are less likely to leave the project.

#  🔌 [Installation](https://github.com/apps/sofiarec)

Sofia is a GitHub application. First, you need to [install](https://github.com/apps/sofiarec) it on your repository or organizational account.

# 📡 Scanning

After installing Sofia, you need to ask Sofia to scan your repository. During the scanning:

 1. Sofia clones your repository to gather commits and all changes.
 2. Sofia fetches all of the merged pull requests and reviewers.
 3. Using commits and reviewers, Sofia builds the knowledge model of your project.
 
After scanning the project, Sofia keeps the model up to date using GitHub WebHooks to analyze events in real time.

### Ask for Scanning

You need to open an Issue and make a comment on it like below. Instead of the **master**, specify your main branch.

```
@SofiaRec scan branch master
```

![Image description](https://raw.githubusercontent.com/mirsaeedi/Sofia/master/src/Sofia/wwwroot/img/scan.PNG)

A full scan usually takes around 2-3 hours for large repositories. Upon completion, Sofia lets you know by leaving a comment.

# 📣 Get Suggestions

You can ask for suggestions, once the scanning is complete. Simply, ask for Sofia's recommendation by making the following comment on your pull request.

```
@SofiaRec suggest learners
```

![Image description](https://raw.githubusercontent.com/mirsaeedi/Sofia/master/src/Sofia/wwwroot/img/suggestions.PNG)


