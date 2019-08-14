using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Octokit.Bot;

namespace Sophia.WebHooksHandling.Commands
{
    public static class CommandHandlerFactory
    {
        private static Lazy<NullCommandHandler> _nullCommandHandler = new Lazy<NullCommandHandler>(()=>new NullCommandHandler());

        private static ConcurrentBag<ICommandHandler> _commandHandlers = new ConcurrentBag<ICommandHandler>();

        internal static ICommandHandler GetHandler(string action, string[] parts, string authorAssociation, EventContext eventContext)
        {
            foreach (var commandHandler in _commandHandlers)
            {
                if (commandHandler.IsMatch(action,parts,authorAssociation,eventContext))
                {
                    return commandHandler;
                }
            }

            return _nullCommandHandler.Value;
        }

        public static void RegisterCommandHandler(ICommandHandler commandHandler)
        {
            _commandHandlers.Add(commandHandler);
        }

        public static void RegisterCommandHandlers()
        {
            var itype = typeof(ICommandHandler);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => itype.IsAssignableFrom(p) && itype!=p);

            foreach (var type in types)
            {
                RegisterCommandHandler(Activator.CreateInstance(type) as ICommandHandler);
            }
        }
    }
}
