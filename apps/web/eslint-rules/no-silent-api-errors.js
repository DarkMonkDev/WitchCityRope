/**
 * ESLint Rule: no-silent-api-errors
 *
 * Prevents silent API error handling patterns that mask real problems:
 * - Returning empty values on 404 errors without proper justification
 * - Using console.warn instead of throwing errors in API handlers
 * - Swallowing errors in mutation onError handlers without user notification
 *
 * Created: 2025-10-09
 * Context: After removing 5 instances of silent 404 handlers that masked API/database issues
 *
 * @see /apps/web/eslint-rules/README.md for full documentation
 */

export default {
  meta: {
    type: 'problem',
    docs: {
      description: 'Detect and prevent silent API error handling patterns',
      category: 'Best Practices',
      recommended: true,
      url: 'https://github.com/DarkMonkDev/WitchCityRope/blob/main/apps/web/eslint-rules/README.md'
    },
    messages: {
      silentReturn404: 'Silent 404 return detected. Errors should be thrown or logged, not silently suppressed. If checking existence, document with a comment: "// Intentional: Checking existence"',
      consoleWarnInCatch: 'Using console.warn in catch block masks API errors. Use console.error and throw, or notify user with toast/alert.',
      silentMutationError: 'Mutation onError handler without user notification. Users must be informed when operations fail via toast, alert, or error boundary.',
      emptyReturnOnError: 'Returning empty array/object in catch block masks API failures. Let errors propagate to error boundaries or handle explicitly with user notification.',
    },
    schema: [
      {
        type: 'object',
        properties: {
          allowedFunctions: {
            type: 'array',
            description: 'Function names allowed to return null/empty on 404 (e.g., existence checks)',
            items: { type: 'string' }
          },
          allowConsoleInDev: {
            type: 'boolean',
            description: 'Allow console.warn in development (default: false)',
            default: false
          }
        },
        additionalProperties: false
      }
    ],
    fixable: null,
  },

  create(context) {
    const options = context.options[0] || {};
    const allowedFunctions = options.allowedFunctions || [];
    const allowConsoleInDev = options.allowConsoleInDev || false;

    /**
     * Check if current function is in allowed list
     */
    function isAllowedFunction(node) {
      let functionNode = node;

      // Traverse up to find parent function/method
      while (functionNode &&
             functionNode.type !== 'FunctionDeclaration' &&
             functionNode.type !== 'FunctionExpression' &&
             functionNode.type !== 'ArrowFunctionExpression' &&
             functionNode.type !== 'MethodDefinition') {
        functionNode = functionNode.parent;
      }

      if (!functionNode) return false;

      // Get function name
      const functionName =
        functionNode.id?.name ||
        functionNode.key?.name ||
        (functionNode.parent?.type === 'VariableDeclarator' ? functionNode.parent.id.name : null);

      return functionName && allowedFunctions.includes(functionName);
    }

    /**
     * Check if code has a justification comment
     */
    function hasJustificationComment(node) {
      const sourceCode = context.getSourceCode();
      const comments = sourceCode.getCommentsBefore(node);

      const justificationPatterns = [
        /intentional/i,
        /checking existence/i,
        /expected behavior/i,
        /by design/i,
        /legitimate null/i,
      ];

      return comments.some(comment =>
        justificationPatterns.some(pattern => pattern.test(comment.value))
      );
    }

    /**
     * Check if return statement is problematic silent 404 return
     */
    function checkReturn404Pattern(node) {
      // Looking for pattern:
      // if (error.response?.status === 404) { return; }
      // if (error.response?.status === 404) { return {}; }
      // if (error.response?.status === 404) { return []; }

      if (node.type !== 'IfStatement') return;

      const test = node.test;

      // Check if condition checks for 404 status
      const is404Check =
        (test.type === 'BinaryExpression' &&
         test.operator === '===' &&
         test.right.type === 'Literal' &&
         test.right.value === 404) ||
        (test.type === 'LogicalExpression' &&
         test.operator === '&&' &&
         test.right.type === 'BinaryExpression' &&
         test.right.right.value === 404);

      if (!is404Check) return;

      // Get the consequent (what happens if condition is true)
      const consequent = node.consequent.type === 'BlockStatement'
        ? node.consequent.body[0]
        : node.consequent;

      if (!consequent || consequent.type !== 'ReturnStatement') return;

      // Check what's being returned
      const returnValue = consequent.argument;
      const isEmptyReturn = !returnValue;
      const isEmptyObject = returnValue?.type === 'ObjectExpression' && returnValue.properties.length === 0;
      const isEmptyArray = returnValue?.type === 'ArrayExpression' && returnValue.elements.length === 0;
      const isNull = returnValue?.type === 'Literal' && returnValue.value === null;

      if (isEmptyReturn || isEmptyObject || isEmptyArray) {
        // Check for justification or allowed function
        if (isAllowedFunction(node) || hasJustificationComment(node)) {
          return;
        }

        context.report({
          node,
          messageId: 'silentReturn404',
        });
      }

      // Null returns are OK if justified (existence checks)
      if (isNull && !hasJustificationComment(node) && !isAllowedFunction(node)) {
        context.report({
          node,
          messageId: 'silentReturn404',
        });
      }
    }

    /**
     * Check for console.warn in catch blocks
     */
    function checkConsoleWarnInCatch(node) {
      // Looking for: catch (error) { console.warn(...); return mockData; }

      if (node.type !== 'CatchClause') return;

      const catchBody = node.body.body;
      if (!catchBody || catchBody.length === 0) return;

      let hasConsoleWarn = false;
      let hasReturn = false;

      catchBody.forEach(statement => {
        // Check for console.warn call
        if (statement.type === 'ExpressionStatement' &&
            statement.expression.type === 'CallExpression') {
          const callee = statement.expression.callee;

          if (callee.type === 'MemberExpression' &&
              callee.object.name === 'console' &&
              callee.property.name === 'warn') {
            hasConsoleWarn = true;
          }
        }

        // Check for return statement
        if (statement.type === 'ReturnStatement') {
          hasReturn = true;
        }
      });

      // Report if using console.warn with return (masking error)
      if (hasConsoleWarn && hasReturn && !allowConsoleInDev) {
        if (hasJustificationComment(node)) return;

        context.report({
          node,
          messageId: 'consoleWarnInCatch',
        });
      }
    }

    /**
     * Check for empty returns in catch blocks
     */
    function checkEmptyReturnInCatch(node) {
      // Looking for: catch (error) { return []; } or catch (error) { return {}; }

      if (node.type !== 'CatchClause') return;

      const catchBody = node.body.body;
      if (!catchBody || catchBody.length === 0) return;

      catchBody.forEach(statement => {
        if (statement.type === 'ReturnStatement' && statement.argument) {
          const returnValue = statement.argument;

          const isEmptyArray = returnValue.type === 'ArrayExpression' &&
                               returnValue.elements.length === 0;
          const isEmptyObject = returnValue.type === 'ObjectExpression' &&
                               returnValue.properties.length === 0;

          if ((isEmptyArray || isEmptyObject) &&
              !hasJustificationComment(node) &&
              !isAllowedFunction(node)) {
            context.report({
              node: statement,
              messageId: 'emptyReturnOnError',
            });
          }
        }
      });
    }

    /**
     * Check for silent mutation onError handlers
     */
    function checkMutationOnError(node) {
      // Looking for: useMutation({ onError: (error) => { console.log(error); } })

      if (node.type !== 'CallExpression') return;

      const callee = node.callee;
      if (callee.type !== 'Identifier' || callee.name !== 'useMutation') return;

      const configArg = node.arguments[0];
      if (!configArg || configArg.type !== 'ObjectExpression') return;

      // Find onError property
      const onErrorProp = configArg.properties.find(prop =>
        prop.key && prop.key.name === 'onError'
      );

      if (!onErrorProp) return;

      const onErrorValue = onErrorProp.value;
      let onErrorBody;

      // Handle different function syntaxes
      if (onErrorValue.type === 'ArrowFunctionExpression') {
        onErrorBody = onErrorValue.body.type === 'BlockStatement'
          ? onErrorValue.body.body
          : [onErrorValue.body];
      } else if (onErrorValue.type === 'FunctionExpression') {
        onErrorBody = onErrorValue.body.body;
      } else {
        return;
      }

      if (!onErrorBody || onErrorBody.length === 0) return;

      // Check if onError only logs without user notification
      const hasOnlyConsoleLog = onErrorBody.every(statement => {
        if (statement.type === 'ExpressionStatement') {
          const expr = statement.expression;
          if (expr.type === 'CallExpression' &&
              expr.callee.type === 'MemberExpression' &&
              expr.callee.object.name === 'console') {
            return true;
          }
        }
        return false;
      });

      if (hasOnlyConsoleLog) {
        context.report({
          node: onErrorProp,
          messageId: 'silentMutationError',
        });
      }
    }

    return {
      IfStatement: checkReturn404Pattern,
      CatchClause(node) {
        checkConsoleWarnInCatch(node);
        checkEmptyReturnInCatch(node);
      },
      CallExpression: checkMutationOnError,
    };
  },
};
